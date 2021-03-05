// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageMapProvider
    {
        private const string PackageMapExtension = "*.json";
        private const string PackageUpdaterOptionsSectionName = "PackageUpdater";

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private readonly AggregateExtension _extensions;
        private readonly ILogger<PackageMapProvider> _logger;

        public PackageMapProvider(AggregateExtension extensions, ILogger<PackageMapProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<NuGetPackageMap> GetPackageMapsAsync([EnumeratorCancellation] CancellationToken token)
        {
            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for package maps in {Extension}", extension.Name);

                var packageMapPath = extension.GetOptions<PackageUpdaterOptions>(PackageUpdaterOptionsSectionName)?.PackageMapPath;

                if (packageMapPath is not null)
                {
                    foreach (var file in extension.GetFiles(packageMapPath, PackageMapExtension))
                    {
                        IEnumerable<NuGetPackageMap?>? newMaps = null;
                        try
                        {
                            using var config = File.OpenRead(file);
                            newMaps = await JsonSerializer.DeserializeAsync<IEnumerable<NuGetPackageMap>>(config, JsonSerializerOptions, token).ConfigureAwait(false);
                        }
                        catch (JsonException exc)
                        {
                            _logger.LogDebug(exc, "File {PackageMapPath} is not a valid package map file", file);
                        }

                        if (newMaps != null)
                        {
                            foreach (var map in newMaps.Where(m => m is not null))
                            {
                                yield return map!;
                            }

                            _logger.LogDebug("Loaded {MapCount} package maps from {PackageMapPath}", newMaps.Count(), file);
                        }
                    }
                }

                _logger.LogDebug("Finished loading package maps from extension {Extension}", extension.Name);
            }
        }
    }
}
