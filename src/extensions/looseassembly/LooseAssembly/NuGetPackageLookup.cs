// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    public sealed class NuGetPackageLookup : IDisposable
    {
        private readonly IPackageLoader _packages;
        private readonly ILogger<NuGetPackageLookup> _logger;
        private readonly Lazy<List<NuGetPackageLookupIndex>> _indexes;

        public NuGetPackageLookup(
            IPackageLoader packages,
            IOptions<ICollection<LooseDependencyOptions>> options,
            ILogger<NuGetPackageLookup> logger)
        {
            _packages = packages;
            _logger = logger;
            _indexes = new Lazy<List<NuGetPackageLookupIndex>>(() =>
            {
                var list = new List<NuGetPackageLookupIndex>();

                foreach (var option in options.Value)
                {
                    foreach (var path in option.Indexes)
                    {
                        try
                        {
                            var file = option.Files.GetFileInfo(path);

                            if (!file.Exists)
                            {
                                logger.LogWarning("Could not find index file at {Path}", path);
                            }
                            else if (file.PhysicalPath is not null)
                            {
                                logger.LogDebug("Loading index file from {Path}", file.PhysicalPath);
                                list.Add(new NuGetPackageLookupIndex(file.PhysicalPath));
                            }
                            else
                            {
                                logger.LogDebug("Loading index file from {Path}", path);
                                logger.LogWarning("Currently only physical paths are supported. Copying to a temporary path....");
                                list.Add(new TemporaryFileNuGetPackageLookupIndex(file));
                                logger.LogDebug("Done creating temporary copy");
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Could not load loose assembly index {Path}", path);
                        }
                    }
                }

                return list;
            });
        }

        public async IAsyncEnumerable<NuGetReference> SearchAsync(string path, IEnumerable<TargetFrameworkMoniker> tfms, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var index in _indexes.Value)
            {
                index.FindNuGetPackageInfoForFile(path, out var ownerPackageId, out var containingPackage);

                var result = (ownerPackageId, containingPackage) switch
                {
                    { containingPackage: { Id: var id }, } when string.Equals(id, ownerPackageId, StringComparison.OrdinalIgnoreCase) => GetNuGetReference(containingPackage),
                    { containingPackage: { Id: var id }, ownerPackageId: not null } when !string.Equals(id, ownerPackageId, StringComparison.OrdinalIgnoreCase) => await GetLatestReferenceAsync(ownerPackageId, containingPackage),
                    { containingPackage: not null, ownerPackageId: null } => GetNuGetReference(containingPackage),
                    { containingPackage: null, ownerPackageId: not null } => await GetLatestReferenceAsync(ownerPackageId, containingPackage!),
                    _ => null,
                };

                if (result is not null)
                {
                    yield return result;
                }
            }

            async Task<NuGetReference> GetLatestReferenceAsync(string packageId, NuGetPackageVersion nuget)
            {
                _logger.LogDebug("Searching for the latest version of {PackageId} for {TFMs}", packageId, tfms);
                var latest = await _packages.GetLatestVersionAsync(packageId, tfms, new() { Prerelease = true, Unlisted = true }, token);

                if (latest is not null)
                {
                    return latest;
                }

                // Default to returning the found package if nothing else matches
                return GetNuGetReference(nuget);
            }

            [return: NotNullIfNotNull("version")]
            static NuGetReference? GetNuGetReference(NuGetPackageVersion? version)
                => version is null ? null : new(version.Id, version.Version);
        }

        public void Dispose()
        {
            if (_indexes.IsValueCreated)
            {
                foreach (var index in _indexes.Value)
                {
                    index.Dispose();
                }
            }
        }
    }
}
