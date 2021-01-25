using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AspNetMigrator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConfigUpdater
{
    public class ConfigUpdaterProvider
    {
        private const string AssemblySearchPattern = "*.dll";
        private const string ConfigFilePathsSettingName = "ConfigFilePaths";
        private const string ConfigUpdaterPathSettingName = "ConfigUpdaterPath";

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
            _extensions.GetSetting(ConfigFilePathsSettingName)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();

        public IEnumerable<IConfigUpdater> GetUpdaters()
        {
            var updaters = new List<IConfigUpdater>();

            foreach (var extension in _extensions.ExtensionProviders)
            {
                _logger.LogDebug("Looking for config updates in {Extension}", extension.Name);

                var configUpdaterPath = extension.GetSetting(ConfigUpdaterPathSettingName);

                if (configUpdaterPath is null)
                {
                    continue;
                }

                foreach (var file in extension.ListFiles(configUpdaterPath, AssemblySearchPattern))
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
            }

            _logger.LogDebug("Loaded {Count} config updaters", updaters.Count);

            return updaters;
        }
    }
}
