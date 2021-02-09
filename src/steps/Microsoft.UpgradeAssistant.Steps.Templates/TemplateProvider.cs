using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UpgradeAssistant.Extensions;

namespace Microsoft.UpgradeAssistant.Steps.Templates
{
    public class TemplateProvider
    {
        private const string TemplateConfigFileName = "TemplateConfig.json";
        private const string TemplateInserterOptionsSectionName = "TemplateInserter";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringProjectItemTypeConverter() }
        };

        private readonly AggregateExtensionProvider _extensions;
        private readonly ILogger<TemplateProvider> _logger;
        private readonly Dictionary<IExtensionProvider, IEnumerable<string>> _templateConfigFiles;

        public IEnumerable<string> TemplateConfigFileNames => _templateConfigFiles.SelectMany(kvp => kvp.Value.Select(c => $"{kvp.Key.Name}:{c}"));

        public TemplateProvider(AggregateExtensionProvider extensions, ILogger<TemplateProvider> logger)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _templateConfigFiles = _extensions.ExtensionProviders.ToDictionary(e => e, e => GetTemplateConfigFiles(e));
        }

        internal async Task<Dictionary<string, RuntimeItemSpec>> GetTemplatesAsync(bool isWebApp, CancellationToken token)
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

                    // If there was a problem reading the configuration or the configuration only applies to web apps and the
                    // current project isn't a web app, continue to the next config file.
                    if (templateConfig?.TemplateItems is null || (!isWebApp && templateConfig.UpdateWebAppsOnly))
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

        private async Task<TemplateConfiguration?> LoadTemplateConfigurationAsync(IExtensionProvider extension, string path, CancellationToken token)
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
        private IEnumerable<string> GetTemplateConfigFiles(IExtensionProvider extension)
        {
            _logger.LogDebug("Looking for template config files in extension {Extension}", extension.Name);

            var options = extension.GetOptions<TemplateInserterOptions>(TemplateInserterOptionsSectionName);

            return options?.TemplatePath is null ? Enumerable.Empty<string>() : extension.GetFiles(options.TemplatePath, TemplateConfigFileName);
        }
    }
}
