// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class NuGetExtensionDownloader : IExtensionDownloader
    {
        private readonly IOptions<ExtensionOptions> _options;
        private readonly IExtensionLocator _extensionLocator;
        private readonly Lazy<IPackageDownloader> _packageDownloader;
        private readonly ILogger<NuGetExtensionDownloader> _logger;

        public NuGetExtensionDownloader(
            IOptions<ExtensionOptions> options,
            IExtensionLocator extensionLocator,
            Lazy<IPackageDownloader> packageDownloader,
            ILogger<NuGetExtensionDownloader> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _extensionLocator = extensionLocator ?? throw new ArgumentNullException(nameof(extensionLocator));
            _packageDownloader = packageDownloader ?? throw new ArgumentNullException(nameof(packageDownloader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetLatestVersionAsync(ExtensionSource n, CancellationToken token)
        {
            if (n is null)
            {
                throw new ArgumentNullException(nameof(n));
            }

            var result = await _packageDownloader.Value.GetNuGetReference(n.Name, n.Version, n.Source ?? _options.Value.DefaultSource, token).ConfigureAwait(false);

            return result?.Version;
        }

        public async Task<string?> RestoreAsync(ExtensionSource source, CancellationToken token)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(source.Version))
            {
                _logger.LogWarning("Cannot restore a source {Name} with no version", source.Name);
                return null;
            }

            var path = _extensionLocator.GetInstallPath(source);

            if (Directory.Exists(path))
            {
                _logger.LogDebug("{Name} is already restored at {Path}", source.Name, path);
                return path;
            }

            _logger.LogDebug("Creating directory {Path} for extension {Name}", path, source.Name);

            Directory.CreateDirectory(path);
            var package = new NuGetReference(source.Name, source.Version);

            if (await _packageDownloader.Value.DownloadPackageToDirectoryAsync(path, package, source.Source ?? _options.Value.DefaultSource, token).ConfigureAwait(false))
            {
                return path;
            }

            Directory.Delete(path, true);

            return null;
        }
    }
}
