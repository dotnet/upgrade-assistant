using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        private static readonly Regex PropertyRegex = new(@"^\$\((.*)\)$", RegexOptions.Compiled);

        // Files that indicate the project is likely a web app rather than a class library or some other project type
        private static readonly ItemSpec[] WebAppFiles = new[]
        {
            new ItemSpec(ProjectItemType.Content, "Global.asax", Array.Empty<string>()),
            new ItemSpec(ProjectItemType.Content, "Web.config", Array.Empty<string>())
        };

        private readonly TemplateProvider _templateProvider;

        private Dictionary<string, RuntimeItemSpec> _itemsToAdd;

        public override string Id => typeof(TemplateInserterStep).FullName!;

        public override string Description => $"Add template files (for startup code paths, for example) based on template files described in: {string.Join(", ", _templateProvider.TemplateConfigFileNames)}";

        public override string Title => $"Add template files";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before adding template files
            "AspNetMigrator.BackupUpdater.BackupStep",

            // Project should be SDK-style before adding template files
            "AspNetMigrator.TryConvertUpdater.TryConvertProjectConverterStep"
        };

        public TemplateInserterStep(TemplateProvider templateProvider, ILogger<TemplateInserterStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
            _itemsToAdd = new Dictionary<string, RuntimeItemSpec>();

            if (!_templateProvider.TemplateConfigFileNames.Any())
            {
                Logger.LogWarning("No template configuration files provided; no template files will be added to project");
            }
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context?.CurrentProject is not null && _templateProvider.TemplateConfigFileNames.Any();

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required().Project;

            try
            {
                _itemsToAdd = (await _templateProvider.GetTemplatesAsync(IsWebApp(project), token).ConfigureAwait(false))
                    .Where(kvp => IsTemplateNeeded(project, kvp.Value))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
                Logger.LogCritical("Invalid project: {ProjectPath}", project.FilePath);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Invalid project: {project.FilePath}", BuildBreakRisk.Unknown);
            }
        }

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required().Project;
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
                    using var templateStream = item.Extension.GetFile(item.TemplateFilePath);
                    if (templateStream is null)
                    {
                        Logger.LogCritical("Expected template {TemplatePath} not found in extension {Extension}", item.Path, item.Extension.Name);
                        return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Expected template {item.Path} not found in extension {item.Extension.Name}");
                    }

                    using var outputStream = File.Create(filePath, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
#pragma warning restore CA2000 // Dispose objects before losing scope

                    await StreamHelpers.CopyStreamWithTokenReplacementAsync(templateStream, outputStream, tokenReplacements).ConfigureAwait(false);
                }
                catch (IOException exc)
                {
                    Logger.LogCritical(exc, "Expected template {TemplatePath} not found in extension {Extension}", item.Path, item.Extension.Name);
                    return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Expected template {item.Path} not found in extension {item.Extension.Name}");
                }

                Logger.LogInformation("Added template file {ItemName} from {Extension}", item.Path, item.Extension.Name);
            }

            // After adding the items on disk, reload the workspace and check whether they were picked up automatically or not
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);
            foreach (var item in _itemsToAdd.Values)
            {
                if (!projectFile.ContainsItem(item.Path, item.Type, token))
                {
                    // Add the new item to the project if it wasn't auto-included
                    projectFile.AddItem(item.Type.Name, item.Path);
                    Logger.LogDebug("Added {ItemName} to project file", item.Path);
                }
            }

            await projectFile.SaveAsync(token).ConfigureAwait(false);

            Logger.LogInformation("{ItemCount} template items added", _itemsToAdd.Count);
            return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"{_itemsToAdd.Count} template items added");
        }

        private bool IsTemplateNeeded(IProject project, RuntimeItemSpec template)
        {
            var candidateMatches = project.FindFiles(template.Type, template.Path);

            if (candidateMatches.Any(path => ItemMatches(path, template)))
            {
                Logger.LogDebug("Template {TemplateItemPath} not needed because the project already contains a similar item", template.Path);
                return false;
            }

            return true;
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
                        string? propertyValue;

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
    }
}
