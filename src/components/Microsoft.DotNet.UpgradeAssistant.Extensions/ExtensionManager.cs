﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class ExtensionManager : IDisposable, IExtensionManager
    {
        private readonly ILogger<ExtensionManager> _logger;
        private readonly Lazy<IEnumerable<ExtensionInstance>> _extensions;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading an extension should not propogate any exceptions.")]
        public ExtensionManager(
            IEnumerable<IExtensionLoader> loaders,
            IOptions<ExtensionOptions> options,
            ILogger<ExtensionManager> logger)
        {
            _logger = logger;
            _extensions = new Lazy<IEnumerable<ExtensionInstance>>(() =>
            {
                var list = new List<ExtensionInstance>();

                foreach (var path in options.Value.ExtensionPaths)
                {
                    LoadPath(path, isDefault: false);
                }

                foreach (var path in options.Value.DefaultExtensions)
                {
                    LoadPath(path, isDefault: true);
                }

                logger.LogInformation("Loaded {Count} extensions", list.Count);

                if (options.Value.AdditionalOptions.Any())
                {
                    list.Add(LoadOptionsExtension(options.Value.AdditionalOptions));
                }

                list.AddRange(options.Value.Extensions);

                return list;

                void LoadPath(string path, bool isDefault)
                {
                    if (LoadExtension(path) is ExtensionInstance extension)
                    {
                        if (isDefault)
                        {
                            extension = extension with { Version = options.Value.CurrentVersion };
                        }

                        if (extension.MinUpgradeAssistantVersion is Version minVersion && minVersion < options.Value.CurrentVersion)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The extension instance will dispose of this.")]
        private static ExtensionInstance LoadOptionsExtension(IEnumerable<AdditionalOption> values)
        {
            var collection = values.Select(v => new KeyValuePair<string, string>(v.Name, v.Value));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(collection)
                .Build();

            return new ExtensionInstance(new PhysicalFileProvider(Environment.CurrentDirectory), Environment.CurrentDirectory, config);
        }

        public bool Remove(string name)
        {
            _logger.LogInformation("Remove Not Implemented: {Name}", name);
            return false;
        }

        public Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token)
        {
            _logger.LogInformation("Update Not Implemented: {Name}", name);
            return Task.FromResult<ExtensionSource?>(null);
        }

        public Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token)
        {
            _logger.LogInformation("Add Not Implemented: {Name} @{Source}", n.Name, n.Source);
            return Task.FromResult<ExtensionSource?>(null);
        }

        public bool TryGetExtension(object service, [MaybeNullWhen(false)] out IExtensionInstance extensionInstance)
        {
            var assembly = service.GetType().Assembly;
            extensionInstance = _extensions.Value.FirstOrDefault(i => i.IsInExtension(assembly));

            return extensionInstance is not null;
        }

        public IEnumerable<IExtensionInstance> Instances => _extensions.Value;

        public IEnumerable<ExtensionSource> Registered => _extensions.Value.Select(v => new ExtensionSource(v.Name) { Source = v.Location });
    }
}
