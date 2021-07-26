// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly IUpgradeAssistantConfigurationLoader _configurationLoader;
        private readonly ILogger<ExtensionManager> _logger;
        private readonly ExtensionOptions _options;
        private readonly Lazy<IEnumerable<ExtensionInstance>> _extensions;

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading an extension should not propagate any exceptions.")]
        public ExtensionManager(
            IEnumerable<IExtensionLoader> loaders,
            IUpgradeAssistantConfigurationLoader configurationLoader,
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

            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value;
            _extensions = new Lazy<IEnumerable<ExtensionInstance>>(() =>
            {
                var list = new List<ExtensionInstance>();

                foreach (var path in _options.ExtensionPaths)
                {
                    LoadPath(path, isDefault: false);
                }

                foreach (var path in _options.DefaultExtensions)
                {
                    LoadPath(path, isDefault: true);
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

            ExtensionInstance? LoadExtension(string path)
            {
                foreach (var loader in loaders)
                {
                    try
                    {
                        if (loader.LoadExtension(path) is ExtensionInstance instance)
                        {
                            if (instance.Version is Version version)
                            {
                                logger.LogDebug("Found extension '{Name}' v{Version} [{Location}]", instance.Name, version, instance.Location);
                            }
                            else
                            {
                                logger.LogDebug("Found extension '{Name}' [{Location}]", instance.Name, instance.Location);
                            }

                            return instance;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "There was an error loading an extension from {Path}", path);
                    }
                }

                return null;
            }
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

            var config = await _configurationLoader.LoadAsync(token).ConfigureAwait(false);

            // TODO: Verify/download/etc
            var updated = config with
            {
                Extensions = config.Extensions.Add(n)
            };

            await _configurationLoader.SaveAsync(updated, token).ConfigureAwait(false);

            return n;
        }

        public bool TryGetExtension(object service, [MaybeNullWhen(false)] out IExtensionInstance extensionInstance)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var assembly = service.GetType().Assembly;
            extensionInstance = _extensions.Value.FirstOrDefault(i => i.IsInExtension(assembly));

            return extensionInstance is not null;
        }

        public IEnumerable<IExtensionInstance> Instances => _extensions.Value;

        public IEnumerable<ExtensionSource> Registered => _extensions.Value.Select(v => new ExtensionSource(v.Name) { Source = v.Location });
    }
}
