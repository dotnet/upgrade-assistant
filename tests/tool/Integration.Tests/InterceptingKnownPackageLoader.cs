// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;

namespace Integration.Tests
{
    public class InterceptingKnownPackageLoader : IPackageLoader
    {
        private readonly UnknownPackages _unknownPackages;
        private readonly KnownPackages _packages;
        private readonly IPackageLoader _other;
        private readonly ILogger<InterceptingKnownPackageLoader> _logger;

        public InterceptingKnownPackageLoader(KnownPackages packages, IPackageLoader other, UnknownPackages unknownPackages, ILogger<InterceptingKnownPackageLoader> logger)
        {
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _other = other ?? throw new ArgumentNullException(nameof(other));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unknownPackages = unknownPackages ?? throw new ArgumentNullException(nameof(unknownPackages));
        }

        public Task<bool> DoesPackageSupportTargetFrameworksAsync(NuGetReference packageReference, IEnumerable<TargetFrameworkMoniker> targetFrameworks, CancellationToken token)
        {
            return _other.DoesPackageSupportTargetFrameworksAsync(packageReference, targetFrameworks, token);
        }

        public async Task<NuGetReference?> GetLatestVersionAsync(string packageName, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, CancellationToken token)
        {
            if (_packages.TryGetValue(packageName, out var known))
            {
                return known;
            }

            var latest = await _other.GetLatestVersionAsync(packageName, tfms, options, token).ConfigureAwait(false);

            if (latest is not null)
            {
                _unknownPackages[packageName] = latest.Version;
                _logger.LogError("Unexpected version: {Name}, {Version}", latest.Name, latest.Version);
            }

            return latest;
        }

        public async IAsyncEnumerable<NuGetReference> GetNewerVersionsAsync(NuGetReference reference, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, [EnumeratorCancellation] CancellationToken token)
        {
            if (_packages.TryGetValue(reference.Name, out var known))
            {
                yield return known;
                yield break;
            }

            var latest = _other.GetNewerVersionsAsync(reference, tfms, options, token);
            var last = await latest.LastAsync(token).ConfigureAwait(false);
            if (latest is NuGetReference latestReference)
            {
                _unknownPackages[latestReference.Name] = latestReference.Version;
                _logger.LogError("Unexpected check for newer version: {Name}, {Version}", reference.Name, reference.Version);
            }

            await foreach (var l in latest.WithCancellation(token))
            {
                yield return l;
            }
        }

        public Task<NuGetPackageMetadata?> GetPackageMetadata(NuGetReference reference, CancellationToken token)
            => _other.GetPackageMetadata(reference, token);
    }
}
