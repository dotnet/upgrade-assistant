// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateProvider
    {
        private const string TemplateInserterOptionsSectionName = "TemplateInserter";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringProjectItemTypeConverter(), new JsonStringEnumConverter() }
        };

        private readonly AggregateExtension _extensions;
        private readonly ILogger<TemplateProvider> _logger;
        private readonly Dictionary<IExtension, IEnumerable<string>> _templateConfigFiles;

        public IEnumerable<string> TemplateConfigFileNames => _templateConfigFiles.SelectMany(kvp => kvp.Value.Select(c => $"{kvp.Key.Name}:{c}"));

        public TemplateProvider(AggregateExtension extensions, ILogger<TemplateProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _templateConfigFiles = _extensions.ExtensionProviders.ToDictionary(e => e, e => GetTemplateConfigFiles(e));
        }

        internal async Task<Dictionary<string, RuntimeItemSpec>> GetTemplatesAsync(IProject project, CancellationToken token)
        {
            var templates = new Dictionary<string, RuntimeItemSpec>();

            // Iterate through all extensions' config files, adding template files from each to the list of items to add, as appropriate.
            // Later extensions can intentionally overwrite earlier extensions' items.
            foreach (var extension in _extensions.ExtensionProviders)
            {
                foreach (var configFile in _templateConfigFiles[extension])
                {
                    var configFilePath = Path.GetDirectoryName(configFile) ?? string.Empty;
                    var templateConfig = await LoadTemplateConfigurationAsync(extension, configFile, token).ConfigureAwait(false);

                    // If there was a problem reading the configuration or the configuration only applies to certain output types
                    // or project types which don't match the project, then continue to the next configuration.
                    if (templateConfig?.TemplateItems is null || !await templateConfig.AppliesToProject(project, token).ConfigureAwait(false))
                    {
                        _logger.LogDebug("Skipping inapplicable template config file {TemplateConfigFile}", configFile);
                        continue;
                    }

                    _logger.LogDebug("Loaded {ItemCount} template items from template config file {TemplateConfigFile}", templateConfig.TemplateItems?.Length ?? 0, configFile);

                    foreach (var templateItem in templateConfig.TemplateItems!)
                    {
                        templates[templateItem.Path] = new RuntimeItemSpec(templateItem, extension, Path.Combine(configFilePath, templateItem.Path), templateConfig.Replacements ?? new Dictionary<string, string>());
                    }
                }
            }

            return templates;
        }

        private async Task<TemplateConfiguration?> LoadTemplateConfigurationAsync(IExtension extension, string path, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogError("Invalid template configuration file path: {TemplateConfigPath}", path);
                return null;
            }

            try
            {
                using var config = extension.GetFile(path);
                if (config is null)
                {
                    _logger.LogError("File {Path} could not be read from extension {Extension}", path, extension.Name);
                    return null;
                }
                else
                {
                    return await JsonSerializer.DeserializeAsync<TemplateConfiguration>(config, JsonOptions, cancellationToken: token).ConfigureAwait(false);
                }
            }
            catch (JsonException)
            {
                _logger.LogError("Error deserializing template configuration file: {TemplateConfigPath}", path);
                return null;
            }
        }

        /// <summary>
        /// Gets template config files in an extension location.
        /// </summary>
        /// <param name="extension">The extension provider to look in for template config files.</param>
        /// <returns>Paths to any template config files in the extension.</returns>
        private IEnumerable<string> GetTemplateConfigFiles(IExtension extension)
        {
            _logger.LogDebug("Looking for template config files in extension {Extension}", extension.Name);
            var options = extension.GetOptions<TemplateInserterOptions>(TemplateInserterOptionsSectionName);
            var configFiles = options?.TemplateConfigFiles ?? Enumerable.Empty<string>();
            _logger.LogDebug("Found {TemplateCount} template config files in extension {Extension}", configFiles.Count(), extension.Name);
            return configFiles;
        }
    }
}
