// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigUpdaterProvider
    {
        private const string AssemblySearchPattern = "*.dll";
        private const string ConfigUpdaterOptionsSectionName = "ConfigUpdater";

        private readonly AggregateExtensionProvider _extensions;
        private readonly ILogger<ConfigUpdaterProvider> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ConfigUpdaterProvider(AggregateExtensionProvider extensions, IServiceProvider serviceProvider, ILogger<ConfigUpdaterProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<string> ConfigFilePaths =>
            _extensions.GetOptions<ConfigUpdaterOptions>(ConfigUpdaterOptionsSectionName)?.ConfigFilePaths ?? Enumerable.Empty<string>();

        public IEnumerable<IConfigUpdater> GetUpdaters()
        {
            var updaters = new List<IConfigUpdater>();

            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for config updaters in {Extension}", extension.Name);

                var configUpdaterOptions = extension.GetOptions<ConfigUpdaterOptions>(ConfigUpdaterOptionsSectionName);

                if (configUpdaterOptions?.ConfigUpdaterPath is null)
                {
                    _logger.LogDebug("No config updater section in extension manifest. Finished loading config updaters from {Extension}", extension.Name);
                    continue;
                }

                foreach (var file in extension.GetFiles(configUpdaterOptions.ConfigUpdaterPath, AssemblySearchPattern))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);

                        var newUpdaters = assembly.GetTypes()
                            .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IConfigUpdater)))
                            .Select(t => ActivatorUtilities.CreateInstance(_serviceProvider, t))
                            .Cast<IConfigUpdater>();

                        _logger.LogDebug("Loaded {Count} config updaters from {AssemblyName}", newUpdaters.Count(), assembly.FullName);
                        updaters.AddRange(newUpdaters);
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }

                _logger.LogDebug("Finished loading config updaters from {Extension}", extension.Name);
            }

            _logger.LogDebug("Loaded {Count} config updaters", updaters.Count);

            return updaters;
        }
    }
}
