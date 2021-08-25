// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public sealed class ExtensionManager : IExtensionManager
    {
        private readonly Lazy<IExtensionDownloader> _extensionDownloader;
        private readonly ILogger<ExtensionManager> _logger;
        private readonly IExtensionProvider _extensionProvider;
        private readonly IUpgradeAssistantConfigurationLoader _configurationLoader;

        public ExtensionManager(
            IExtensionProvider extensionProvider,
            IUpgradeAssistantConfigurationLoader configurationLoader,
            Lazy<IExtensionDownloader> extensionDownloader,
            ILogger<ExtensionManager> logger)
        {
            _extensionProvider = extensionProvider ?? throw new ArgumentNullException(nameof(extensionProvider));
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _extensionDownloader = extensionDownloader ?? throw new ArgumentNullException(nameof(extensionDownloader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> RemoveAsync(string name, CancellationToken token)
        {
            var config = _configurationLoader.Load();
            var existing = config.Extensions.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                return Task.FromResult(false);
            }

            var updated = config with
            {
                Extensions = config.Extensions.Remove(existing)
            };

            _configurationLoader.Save(updated);

            return Task.FromResult(true);
        }

        public async Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token)
        {
            var config = _configurationLoader.Load();
            var existing = config.Extensions.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                _logger.LogWarning("Extension '{Name}' is not installed.", name);
                return null;
            }

            var latestVersion = await _extensionDownloader.Value.GetLatestVersionAsync(existing with { Version = null }, token).ConfigureAwait(false);

            if (latestVersion is null)
            {
                _logger.LogWarning("Could not find extension '{Name}' in source '{Source}'", existing.Name, existing.Source);
                return null;
            }

            var latest = existing with { Version = latestVersion };

            _logger.LogInformation("Updating to latest version {Version} of extension '{Extension}'", latest.Version, latest.Name);

            await _extensionDownloader.Value.RestoreAsync(latest, token).ConfigureAwait(false);

            var updated = config with
            {
                Extensions = config.Extensions.Replace(existing, latest)
            };

            _configurationLoader.Save(updated);

            return latest;
        }

        public async Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token)
        {
            if (n is null)
            {
                throw new ArgumentNullException(nameof(n));
            }

            if (string.IsNullOrEmpty(n.Version))
            {
                var version = await _extensionDownloader.Value.GetLatestVersionAsync(n, token).ConfigureAwait(false);

                if (version is null)
                {
                    _logger.LogError("Could not find a version for extension {Name}", n.Name);
                    return null;
                }

                n = n with { Version = version };

                _logger.LogInformation("Found version for extension {Name}: {Version}", n.Name, n.Version);
            }

            var path = await _extensionDownloader.Value.RestoreAsync(n, token).ConfigureAwait(false);

            if (path is null)
            {
                _logger.LogError("Could not install extension {Name} [{Version}] from {Source}", n.Name, n.Version, n.Source);
                return null;
            }

            var config = _configurationLoader.Load();

            var updated = config with
            {
                Extensions = config.Extensions.Add(n)
            };

            _configurationLoader.Save(updated);

            return n;
        }

        public async Task<bool> RestoreExtensionsAsync(CancellationToken token)
        {
            var success = true;

            foreach (var extension in _extensionProvider.Registered)
            {
                _logger.LogInformation("Restoring {Extension}", extension.Name);

                if (await _extensionDownloader.Value.RestoreAsync(extension, token).ConfigureAwait(false) is null)
                {
                    success = false;
                }
            }

            return success;
        }
    }
}
