// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
                        list.Add(extension);
                    }
                    else
                    {
                        logger.LogWarning("Could not load extension from {Path}", path);
                    }
                }

                logger.LogInformation("Loaded {Count} extensions", list.Count);

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
                            logger.LogDebug("Loaded extension from {Path}", path);
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

        public IEnumerator<ExtensionInstance> GetEnumerator() => _extensions.Value.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
