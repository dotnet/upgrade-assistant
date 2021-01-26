using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Extensions;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.PackageUpdater
{
    public class PackageMapProvider
    {
        private const string PackageMapExtension = "*.json";
        private const string PackageUpdaterOptionsSectionName = "PackageUpdaterOptions";

        private readonly AggregateExtensionProvider _extensions;
        private readonly ILogger<PackageMapProvider> _logger;

        public PackageMapProvider(AggregateExtensionProvider extensions, ILogger<PackageMapProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<NuGetPackageMap>> GetPackageMapsAsync(CancellationToken token)
        {
            var maps = new List<NuGetPackageMap>();

            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for package maps in {Extension}", extension.Name);

                var packageMapPath = extension.GetOptions<PackageUpdaterOptions>(PackageUpdaterOptionsSectionName)?.PackageMapPath;

                if (packageMapPath is not null)
                {
                    foreach (var file in extension.ListFiles(packageMapPath, PackageMapExtension))
                    {
                        try
                        {
                            using var config = File.OpenRead(file);
                            var newMaps = await JsonSerializer.DeserializeAsync<IEnumerable<NuGetPackageMap>>(config, cancellationToken: token).ConfigureAwait(false);
                            if (newMaps != null)
                            {
                                maps.AddRange(newMaps);
                                _logger.LogDebug("Loaded {MapCount} package maps from {PackageMapPath}", newMaps.Count(), file);
                            }
                        }
                        catch (JsonException exc)
                        {
                            _logger.LogDebug(exc, "File {PackageMapPath} is not a valid package map file", file);
                        }
                    }
                }

                _logger.LogDebug("Finished loading package maps from extension {Extension}", extension.Name);
            }

            return maps;
        }
    }
}
