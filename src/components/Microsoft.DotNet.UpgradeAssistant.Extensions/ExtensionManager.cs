// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public sealed class ExtensionManager : IDisposable, IExtensionManager
    {
        private readonly Lazy<IPackageDownloader> _packageDownloader;
        private readonly IUpgradeAssistantConfigurationLoader _configurationLoader;
        private readonly IEnumerable<IExtensionLoader> _loaders;
        private readonly ILogger<ExtensionManager> _logger;
        private readonly ExtensionOptions _options;
        private readonly Lazy<IEnumerable<ExtensionInstance>> _extensions;

        public ExtensionManager(
            IEnumerable<IExtensionLoader> loaders,
            IUpgradeAssistantConfigurationLoader configurationLoader,
            Lazy<IPackageDownloader> packageDownloader,
            IOptions<ExtensionOptions> options,
            ILogger<ExtensionManager> logger)
        {
            if (loaders is null)
            {
                throw new ArgumentNullException(nameof(loaders));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _packageDownloader = packageDownloader;
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _loaders = loaders;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value;
            _extensions = new Lazy<IEnumerable<ExtensionInstance>>(() =>
            {
                if (!_options.LoadExtensions)
                {
                    return Enumerable.Empty<ExtensionInstance>();
                }

                var list = new List<ExtensionInstance>();

                foreach (var path in _options.ExtensionPaths)
                {
                    LoadPath(path, isDefault: false);
                }

                foreach (var path in _options.DefaultExtensions)
                {
                    LoadPath(path, isDefault: true);
                }

                try
                {
                    foreach (var path in Registered.Select(GetPath))
                    {
                        LoadPath(path, isDefault: false);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                logger.LogInformation("Loaded {Count} extensions", list.Count);

                if (_options.AdditionalOptions.Any())
                {
                    list.Add(LoadOptionsExtension(_options.AdditionalOptions));
                }

                list.AddRange(_options.Extensions);

                return list;

                void LoadPath(string path, bool isDefault)
                {
                    if (LoadExtension(path) is ExtensionInstance extension)
                    {
                        if (isDefault)
                        {
                            extension = extension with { Version = _options.CurrentVersion };
                        }

                        if (extension.MinUpgradeAssistantVersion is Version minVersion && minVersion < _options.CurrentVersion)
                        {
                            logger.LogWarning("Could not load extension from {Path}. Requires at least v{Version} of Upgrade Assistant.", path, minVersion);
                        }
                        else
                        {
                            list.Add(extension);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Could not load extension from {Path}", path);
                    }
                }
            });
        }

        public void Dispose()
        {
            if (_extensions.IsValueCreated)
            {
                foreach (var extension in _extensions.Value)
                {
                    extension.Dispose();
                }
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The extension instance will dispose of this.")]
        private static ExtensionInstance LoadOptionsExtension(IEnumerable<AdditionalOption> values)
        {
            var collection = values.Select(v => new KeyValuePair<string, string>(v.Name, v.Value));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(collection)
                .Build();

            return new ExtensionInstance(new PhysicalFileProvider(Environment.CurrentDirectory), Environment.CurrentDirectory, config);
        }

        public async Task<bool> RemoveAsync(string name, CancellationToken token)
        {
            var config = await _configurationLoader.LoadAsync(token).ConfigureAwait(false);
            var existing = config.Extensions.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                return false;
            }

            var updated = config with
            {
                Extensions = config.Extensions.Remove(existing)
            };

            await _configurationLoader.SaveAsync(updated, token).ConfigureAwait(false);

            return true;
        }

        public Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token)
        {
            _logger.LogInformation("Update Not Implemented: {Name}", name);
            return Task.FromResult<ExtensionSource?>(null);
        }

        public async Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token)
        {
            if (n is null)
            {
                throw new ArgumentNullException(nameof(n));
            }

            if (string.IsNullOrEmpty(n.Version))
            {
                var result = await _packageDownloader.Value.GetNuGetReference(n.Name, n.Version, n.Source, token).ConfigureAwait(false);

                if (result is null)
                {
                    _logger.LogError("Could not find a version for extension {Name}", n.Name);
                    return null;
                }

                _logger.LogInformation("Found version for extension {Name}: {Version}", n.Name, n.Version);

                n = n with { Version = result.Version };
            }

            var path = await RestoreAsync(n, token).ConfigureAwait(false);

            if (path is null)
            {
                _logger.LogError("Could not install extension {Name} [{Version}] from {Source}", n.Name, n.Version, n.Source);
                return null;
            }

            var config = await _configurationLoader.LoadAsync(token).ConfigureAwait(false);

            var updated = config with
            {
                Extensions = config.Extensions.Add(n)
            };

            await _configurationLoader.SaveAsync(updated, token).ConfigureAwait(false);

            return n;
        }

        public async Task<bool> RestoreExtensionsAsync(CancellationToken token)
        {
            var success = true;

            foreach (var extension in Registered)
            {
                if (await RestoreAsync(extension, token).ConfigureAwait(false) is null)
                {
                    success = false;
                }
            }

            return success;
        }

        private string GetPath(ExtensionSource source)
        {
            if (string.IsNullOrEmpty(source.Version))
            {
                throw new UpgradeException("Cannot get path of source without version");
            }

            return Path.Combine(_options.ExtensionCachePath, source.Name, source.Version);
        }

        private async Task<string?> RestoreAsync(ExtensionSource source, CancellationToken token)
        {
            if (string.IsNullOrEmpty(source.Version))
            {
                _logger.LogWarning("Cannot restore a source {Name} with no version", source.Name);
                return null;
            }

            var path = GetPath(source);

            if (Directory.Exists(path))
            {
                _logger.LogDebug("{Name} is already restored at {Path}", source.Name, path);
                return path;
            }

            _logger.LogDebug("Creating driectory {Path} for extension {Name}", path, source.Name);

            Directory.CreateDirectory(path);
            var package = new NuGetReference(source.Name, source.Version);

            if (await _packageDownloader.Value.DownloadPackageToDirectoryAsync(path, package, source.Source, token).ConfigureAwait(false))
            {
                return path;
            }

            Directory.Delete(path, true);

            return null;
        }

        public bool TryGetExtension(object service, [MaybeNullWhen(false)] out IExtensionInstance extensionInstance)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var assembly = service.GetType().Assembly;
            extensionInstance = Instances.FirstOrDefault(i => i.IsInExtension(assembly));

            return extensionInstance is not null;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading an extension should not propagate any exceptions.")]
        public IExtensionInstance? LoadExtension(string path)
        {
            foreach (var loader in _loaders)
            {
                try
                {
                    if (loader.LoadExtension(path) is ExtensionInstance instance)
                    {
                        if (instance.Version is Version version)
                        {
                            _logger.LogDebug("Found extension '{Name}' v{Version} [{Location}]", instance.Name, version, instance.Location);
                        }
                        else
                        {
                            _logger.LogDebug("Found extension '{Name}' [{Location}]", instance.Name, instance.Location);
                        }

                        return instance;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "There was an error loading an extension from {Path}", path);
                }
            }

            return null;
        }

        public bool CreateExtensionFromDirectory(IExtensionInstance extension, Stream stream)
        {
            if (extension is null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!_packageDownloader.Value.CreateArchive(extension, stream))
            {
                return false;
            }

            stream.Position = 0;

            using var zip = new ZipArchive(stream, ZipArchiveMode.Update);

            foreach (var file in extension.FileProvider.GetFiles("*"))
            {
                var entry = zip.CreateEntry(file.Stem);

                using var entryStream = entry.Open();
                using var fileStream = extension.FileProvider.GetFileInfo(file.Stem).CreateReadStream();

                fileStream.CopyTo(entryStream);
            }

            return true;
        }

        IEnumerable<IExtensionInstance> IExtensionManager.Instances => Instances;

        private IEnumerable<ExtensionInstance> Instances => _extensions.Value;

        public IEnumerable<ExtensionSource> Registered => _configurationLoader.Load().Extensions;
    }
}
