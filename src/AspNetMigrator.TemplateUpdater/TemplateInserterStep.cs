using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetMigrator.TemplateUpdater
{
    /// <summary>
    /// A migration step that adds files from templates if they're
    /// not present in the project. Adds files based on TemplateConfig
    /// files read at runtime.
    /// </summary>
    public class TemplateInserterStep : MigrationStep
    {
        private const int BufferSize = 65536;
        private const string TemplateConfigFileName = "TemplateConfig.json";
        private static readonly Regex PropertyRegex = new(@"^\$\((.*)\)$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringProjectItemTypeConverter() }
        };

        // Files that indicate the project is likely a web app rather than a class library or some other project type
        private static readonly ItemSpec[] WebAppFiles = new[]
        {
            new ItemSpec(ProjectItemType.Content, "Global.asax", Array.Empty<string>()),
            new ItemSpec(ProjectItemType.Content, "Web.config", Array.Empty<string>())
        };

        private readonly IEnumerable<string> _templateConfigFiles;
        private readonly Dictionary<string, RuntimeItemSpec> _itemsToAdd;

        public TemplateInserterStep(MigrateOptions options, IOptions<TemplateInserterStepOptions> templateUpdaterOptions, ILogger<TemplateInserterStep> logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (templateUpdaterOptions is null)
            {
                throw new ArgumentNullException(nameof(templateUpdaterOptions));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _itemsToAdd = new Dictionary<string, RuntimeItemSpec>();
            var templatePath = templateUpdaterOptions.Value.TemplatePath
                ?? throw new ArgumentException("Template inserter options must contain a template path");

            if (!Path.IsPathFullyQualified(templatePath))
            {
                templatePath = Path.Combine(AppContext.BaseDirectory, templatePath);
            }

            _templateConfigFiles = Directory.GetFiles(templatePath, TemplateConfigFileName, new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            });

            if (!_templateConfigFiles.Any())
            {
                Logger.LogWarning("No template configuration files provided; no template files will be added to project");
            }

            Title = $"Add template files";
            Description = $"Add template files (for startup code paths, for example) to {options.ProjectPath} based on template files described in: {string.Join(", ", _templateConfigFiles)}";
        }

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var current = context.Project.Required();

            try
            {
                var isWebApp = IsWebApp(current);

                // Iterate through all config files, adding template files from each to the list of items to add, as appropriate.
                // Later config files can intentionally overwrite earlier config files' items.
                foreach (var templateConfigFile in _templateConfigFiles)
                {
                    var basePath = Path.GetDirectoryName(templateConfigFile) ?? string.Empty;
                    var templateConfiguration = await LoadTemplateConfigurationAsync(templateConfigFile, token).ConfigureAwait(false);

                    // If there was a problem reading the configuration or the configuration only applies to web apps and the
                    // current project isn't a web app, continue to the next config file.
                    if (templateConfiguration?.TemplateItems is null || (!isWebApp && templateConfiguration.UpdateWebAppsOnly))
                    {
                        Logger.LogDebug("Skipping inapplicable template config file {TemplateConfigFile}", templateConfigFile);
                        continue;
                    }

                    Logger.LogDebug("Loaded {ItemCount} template items from template config file {TemplateConfigFile}", templateConfiguration.TemplateItems?.Length ?? 0, templateConfigFile);

                    // Check whether the template items are needed in the project or if they already exist
                    foreach (var templateItem in templateConfiguration.TemplateItems!)
                    {
                        var files = current.FindFiles(templateItem.Type, templateItem.Path);

                        if (files.Any(path => ItemMatches(path, templateItem)))
                        {
                            Logger.LogDebug("Not adding template item {TemplateItemPath} because the project already contains a similar item", templateItem.Path);
                        }
                        else
                        {
                            var templatePath = Path.Combine(basePath, templateItem.Path);
                            if (!File.Exists(templatePath))
                            {
                                Logger.LogError("Template file not found: {TemplateItemPath}", templatePath);
                                continue;
                            }

                            Logger.LogDebug("Marking template item {TemplateItemPath} from template configuration {TemplateConfigFile} for addition", templateItem.Path, templateConfigFile);
                            _itemsToAdd[templateItem.Path] = new RuntimeItemSpec(templateItem, templatePath, templateConfiguration.Replacements ?? new Dictionary<string, string>());
                        }
                    }
                }

                Logger.LogInformation("{FilesNeededCount} expected template items needed", _itemsToAdd.Count);

                if (_itemsToAdd.Any())
                {
                    Logger.LogDebug("Needed items: {NeededFiles}", string.Join(", ", _itemsToAdd.Keys));
                    return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"{_itemsToAdd.Count} expected template items needed ({string.Join(", ", _itemsToAdd.Keys)})", BuildBreakRisk.Medium);
                }
                else
                {
                    return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "All expected template items found", BuildBreakRisk.None);
                }
            }
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", current.FilePath);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Invalid project: {current.FilePath}", BuildBreakRisk.Unknown);
            }
        }

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.Project.Required();
            var projectFile = project.GetFile();

            // For each item to be added, make necessary replacements and then add the item to the project
            foreach (var item in _itemsToAdd.Values)
            {
                var filePath = Path.Combine(project.Directory, item.Path);

                // If the file already exists, move it
                if (File.Exists(filePath))
                {
                    projectFile.RenameFile(filePath);
                }

                // Get the contents of the template file
                try
                {
                    var tokenReplacements = ResolveTokenReplacements(item.Replacements, projectFile);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    using var templateStream = File.Open(item.TemplateFilePath, FileMode.Open, FileAccess.Read);
                    using var outputStream = File.Create(filePath, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
#pragma warning restore CA2000 // Dispose objects before losing scope

                    await StreamHelpers.CopyStreamWithTokenReplacementAsync(templateStream, outputStream, tokenReplacements).ConfigureAwait(false);
                }
                catch (IOException exc)
                {
                    Logger.LogCritical(exc, "Template file not found: {TemplateItemPath}", item.TemplateFilePath);
                    return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Template file not found: {item.TemplateFilePath}");
                }

                if (!projectFile.ContainsItem(item.Path, item.Type, token))
                {
                    // Add the new item to the project if it wasn't auto-included
                    projectFile.AddItem(item.Type.Name, item.Path);
                }

                Logger.LogInformation("Added {ItemName} to the project from template file", item.Path);
            }

            await projectFile.SaveAsync(token).ConfigureAwait(false);

            Logger.LogInformation("{ItemCount} template items added", _itemsToAdd.Count);
            return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"{_itemsToAdd.Count} template items added");
        }

        private Dictionary<string, string> ResolveTokenReplacements(IEnumerable<KeyValuePair<string, string>>? replacements, IProjectFile project)
        {
            var propertyCache = new Dictionary<string, string?>();
            var ret = new Dictionary<string, string>();

            if (replacements is not null)
            {
                foreach (var replacement in replacements)
                {
                    var regexMatch = PropertyRegex.Match(replacement.Value);
                    if (regexMatch.Success)
                    {
                        // If the user specified an MSBuild property as a replacement value ($(...))
                        // then lookup the property value
                        var propertyName = regexMatch.Groups[1].Captures[0].Value;
                        string? propertyValue = null;

                        if (propertyCache.ContainsKey(propertyName))
                        {
                            propertyValue = propertyCache[propertyName];
                        }
                        else
                        {
                            propertyValue = project.GetPropertyValue(propertyName);
                            propertyCache[propertyName] = propertyValue;
                        }

                        if (!string.IsNullOrWhiteSpace(propertyValue))
                        {
                            Logger.LogDebug("Resolved project property {PropertyKey} to {PropertyValue}", propertyName, propertyValue);
                            ret.Add(replacement.Key, propertyValue);
                        }
                        else
                        {
                            Logger.LogWarning("Could not resove project property {PropertyName}; not replacing token {Token}", propertyName, replacement.Key);
                        }
                    }
                    else
                    {
                        // If the replacement value is a string, then just add it directly to the return dictionary
                        ret.Add(replacement.Key, replacement.Value);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Determines if a project is likely to be a web app base on its included items.
        /// </summary>
        private static bool IsWebApp(IProject p)
            => WebAppFiles.Any(file => p.FindFiles(file.Type, file.Path).Any());

        /// <summary>
        /// Determines if a given project element matches an item specification.
        /// </summary>
        private bool ItemMatches(string filePath, ItemSpec expectedItem)
        {
            Logger.LogDebug("Considering {FilePath} for expected file {ExpectedFileName}", filePath, expectedItem.Path);

            // The included file must exist
            if (!File.Exists(filePath))
            {
                Logger.LogDebug("File {FilePath} does not exist", filePath);
                return false;
            }

            // The file must include all specified keywords
            if (expectedItem.Keywords.Any())
            {
                var fileContents = File.ReadAllText(filePath);
                if (expectedItem.Keywords.Any(k => !fileContents.Contains(k, StringComparison.Ordinal)))
                {
                    Logger.LogDebug("File {FilePath} does not contain all necessary keywords to match", filePath);
                    return false;
                }
            }

            Logger.LogDebug("File {FilePath} matches expected file {ExpectedFileName}", filePath, expectedItem.Path);
            return true;
        }

        private async Task<TemplateConfiguration?> LoadTemplateConfigurationAsync(string path, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Logger.LogError("Invalid template configuration file path: {TemplateConfigPath}", path);
                return null;
            }

            if (!File.Exists(path))
            {
                Logger.LogError("Template configuration file not found: {TemplateConfigPath}", path);
                return null;
            }

            try
            {
                using var config = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<TemplateConfiguration>(config, JsonOptions, cancellationToken: token).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                Logger.LogError("Error deserializing template configuration file: {TemplateConfigPath}", path);
                return null;
            }
        }
    }
}
