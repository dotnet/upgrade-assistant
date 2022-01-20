// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public sealed class ExtensionProvider : IDisposable, IExtensionProvider
    {
        private readonly Lazy<IEnumerable<ExtensionInstance>> _extensions;
        private readonly IUpgradeAssistantConfigurationLoader _configurationLoader;
        private readonly IEnumerable<IExtensionLoader> _loaders;
        private readonly ILogger<ExtensionProvider> _logger;
        private readonly ExtensionInstanceFactory _factory;

        public ExtensionProvider(
            IEnumerable<IExtensionLoader> loaders,
            IUpgradeAssistantConfigurationLoader configurationLoader,
            ExtensionInstanceFactory factory,
            IExtensionLocator extensionLocator,
            IOptions<ExtensionOptions> options,
            ILogger<ExtensionProvider> logger)
        {
            if (extensionLocator is null)
            {
                throw new ArgumentNullException(nameof(extensionLocator));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _loaders = loaders ?? throw new ArgumentNullException(nameof(loaders));
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _extensions = new Lazy<IEnumerable<ExtensionInstance>>(() =>
            {
                var list = new List<ExtensionInstance>();

                var opts = options.Value;

                // Required extensions are required for all commands UA may handle, as opposed to other extensions that augment certain commands
                foreach (var path in opts.RequiredExtensions)
                {
                    LoadPath(path, isDefault: true);
                }

                // Required extensions must load, otherwise they may be turned off
                if (!opts.LoadExtensions)
                {
                    return list;
                }

                foreach (var path in opts.DefaultExtensions)
                {
                    LoadPath(path, isDefault: true);
                }

                foreach (var path in opts.ExtensionPaths)
                {
                    LoadPath(path, isDefault: false);
                }

                foreach (var path in Registered.Select(extensionLocator.GetInstallPath))
                {
                    LoadPath(path, isDefault: false);
                }

                logger.LogInformation("Loaded {Count} extensions", list.Count);

                if (opts.AdditionalOptions.Any())
                {
                    list.Add(LoadOptionsExtension(opts.AdditionalOptions));
                }

                list.AddRange(opts.Extensions);

                return list;

                void LoadPath(string path, bool isDefault)
                {
                    if (OpenExtension(path) is ExtensionInstance extension)
                    {
                        if (isDefault)
                        {
                            extension = extension with { Version = opts.CurrentVersion };
                        }

                        if (opts.CheckMinimumVersion && extension.MinUpgradeAssistantVersion is Version minVersion && minVersion > opts.CurrentVersion)
                        {
                            logger.LogWarning("Could not load extension from {Path}. Requires at least v{Version} of Upgrade Assistant and current version is {UpgradeAssistantVersion}.", path, minVersion, opts.CurrentVersion);
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

        private ExtensionInstance LoadOptionsExtension(IEnumerable<AdditionalOption> values)
        {
            var collection = values.Select(v => new KeyValuePair<string, string>(v.Name, v.Value));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(collection)
                .Build();

#pragma warning disable CA2000 // Dispose objects before losing scope
            return _factory.Create(new PhysicalFileProvider(Environment.CurrentDirectory), Environment.CurrentDirectory, config);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public IExtensionInstance? OpenExtension(string path)
        {
            path = Path.GetFullPath(path);

            foreach (var loader in _loaders)
            {
#pragma warning disable CA1031 // Do not catch general exception types
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
                    throw;
                }
            }

            return null;
        }

        IEnumerable<IExtensionInstance> IExtensionProvider.Instances => Instances;

        private IEnumerable<ExtensionInstance> Instances => _extensions.Value;

        public IEnumerable<ExtensionSource> Registered => _configurationLoader.Load().Extensions;
    }
}
