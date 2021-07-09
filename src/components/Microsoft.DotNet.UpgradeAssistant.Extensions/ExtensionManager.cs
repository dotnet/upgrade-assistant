// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal sealed class ExtensionManager : IDisposable, IEnumerable<ExtensionInstance>
    {
        private readonly Lazy<IEnumerable<ExtensionInstance>> _extensions;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading an extension should not propogate any exceptions.")]
        public ExtensionManager(
            IEnumerable<IExtensionLoader> loaders,
            IOptions<ExtensionOptions> options,
            ILogger<ExtensionManager> logger)
        {
            _extensions = new Lazy<IEnumerable<ExtensionInstance>>(() =>
            {
                var list = new List<ExtensionInstance>();

                foreach (var path in options.Value.ExtensionPaths)
                {
                    if (LoadExtension(path) is ExtensionInstance extension)
                    {
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

                logger.LogInformation("Loaded {Count} extensions", list.Count);

                if (options.Value.AdditionalOptions.Any())
                {
                    list.Add(LoadOptionsExtension(options.Value.AdditionalOptions));
                }

                list.AddRange(options.Value.Extensions);

                return list;
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

        public IEnumerator<ExtensionInstance> GetEnumerator() => _extensions.Value.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
