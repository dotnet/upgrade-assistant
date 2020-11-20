using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.StartupUpdater
{
    /// <summary>
    /// A simple migration step that adds files from templates if they're
    /// not present in the project. Adds files necessary for ASP.NET Core app
    /// startup: Program.cs, Startup.cs, appsettings.json, and appsettings.Development.json.
    /// </summary>
    public class StartupUpdaterStep : MigrationStep
    {
        private const string ManifestResourcePrefix = "AspNetMigrator.StartupUpdater.Templates.";
        private const string RootNamespacePropertyName = "RootNamespace";
        private const string TemplateNamespace = "WebApplication1";
        private const int BufferSize = 65536;

        // Files that should be present and text that's expected to be in them
        private static readonly IEnumerable<ItemSpec> ExpectedFiles = new List<ItemSpec>()
        {
            new ItemSpec("Compile", "Program.cs", false, new[] { "Main", "Microsoft.AspNetCore.Hosting" }),
            new ItemSpec("Compile", "Startup.cs", false, new[] { "Configure", "ConfigureServices" }),
            new ItemSpec("Content", "appsettings.json", false, Array.Empty<string>()),
            new ItemSpec("Content", "appsettings.Development.json", false, Array.Empty<string>())
        };

        private List<ItemSpec>? _itemsToAdd;

        public StartupUpdaterStep(MigrateOptions options, ILogger<StartupUpdaterStep> logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Title = $"Update startup code paths";
            Description = $"Add template Program.cs, Startup.cs, and configuration files to {options.ProjectPath}";
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectPath = await context.GetProjectPathAsync(token).ConfigureAwait(false);

            try
            {
                var project = ProjectRootElement.Open(projectPath);
                project.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

                var rootNamespace = project.Properties.FirstOrDefault(p => p.Name.Equals(RootNamespacePropertyName, StringComparison.Ordinal))?.Value;
                if (rootNamespace is null || rootNamespace.Contains("$"))
                {
                    // If there is no root namespace property, default to the project file name
                    rootNamespace = Path.GetFileNameWithoutExtension(projectPath);
                }

                var projectDir = Path.GetDirectoryName(projectPath)!;
                var resourceAssembly = typeof(StartupUpdaterStep).Assembly;

                if (_itemsToAdd is null)
                {
                    throw new InvalidOperationException("Step does not appear initialized");
                }

                // For each file in _itemsToAdd, add the file and do a simple replacement of the template namespace
                // TODO : It will probably worthwhile to make the templating feature more full-featured.
                //        We could prompt users about whether they need different features in their Startup and
                //        include/exclude code based on responses.
                foreach (var item in _itemsToAdd)
                {
                    // Get the path where the file will be added
                    var path = Path.Combine(projectDir, item.ItemName);

                    // If the given file already exists, move it
                    if (File.Exists(path))
                    {
                        RenameFile(path, project);
                    }

                    // Place the specified file
                    using var resourceStream = resourceAssembly.GetManifestResourceStream($"{ManifestResourcePrefix}{item.ItemName}");
                    if (resourceStream is null)
                    {
                        Logger.LogCritical("File resource not found for file {ItemName}", item.ItemName);
                        return (MigrationStepStatus.Failed, $"File resource not found for item {item.ItemName}");
                    }

                    using var inputStream = new StreamReader(resourceStream);

                    // Read the file contents locally to make replacing the template namespace simple.
                    // This is inefficient but makes the code straightforward. If performance becomes an issue
                    // here, we can replace the namespace as we're writing the stream to the new file instead.
                    // That would be faster but would be more complicated and is likely not necessary.
                    // In general, it will be useful to replace this in the future with more full-featured templating code.
                    var fileContents = inputStream.ReadToEnd().Replace(TemplateNamespace, rootNamespace);
                    using var outputStream = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan));
                    await outputStream.WriteAsync(fileContents).ConfigureAwait(false);

                    if (item.IncludeExplicitly)
                    {
                        // Add the new item to the project if it won't be auto-included
                        project.AddItem(item.ItemType, item.ItemName);
                    }

                    Logger.LogInformation("Added {ItemName} to the project from template file", item.ItemName);
                }

                Logger.LogInformation("{ItemCount} items added", _itemsToAdd.Count);

                project.Save();

                return (MigrationStepStatus.Complete, $"{_itemsToAdd.Count} expected startup files added");
            }
            catch (InvalidProjectFileException)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", projectPath);
                return (MigrationStepStatus.Failed, $"Invalid project: {projectPath}");
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectPath = await context.GetProjectPathAsync(token).ConfigureAwait(false);

            if (projectPath is null || !File.Exists(projectPath))
            {
                Logger.LogCritical("Project file {ProjectPath} not found", projectPath);
                return (MigrationStepStatus.Failed, $"Project file {projectPath} not found");
            }

            try
            {
                var projectRoot = ProjectRootElement.Open(projectPath);
                projectRoot.Reload(false); // Reload to make sure we're not seeing an old cached version of the project
                var project = new Project(projectRoot, new Dictionary<string, string>(), null);

                Logger.LogDebug("Scanning project for {ExpectedFileCount} expected files", ExpectedFiles.Count());
                _itemsToAdd = new List<ItemSpec>(ExpectedFiles.Where(e => !project.Items.Any(i => ItemMatches(e, i, projectPath))));
                Logger.LogInformation("{FilesNeededCount} expected startup files needed", _itemsToAdd.Count);

                if (_itemsToAdd.Any())
                {
                    Logger.LogDebug("Needed files: {NeededFiles}", string.Join(", ", _itemsToAdd));
                    return (MigrationStepStatus.Incomplete, $"{_itemsToAdd.Count} expected startup files needed ({string.Join(", ", _itemsToAdd.Select(i => i.ItemName))})");
                }
                else
                {
                    return (MigrationStepStatus.Complete, "Expected startup files found");
                }
            }
            catch (InvalidProjectFileException)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", projectPath);
                return (MigrationStepStatus.Failed, $"Invalid project: {projectPath}");
            }
        }

        /// <summary>
        /// Determines if a given project element matches an item specification.
        /// </summary>
        private bool ItemMatches(ItemSpec expectedItem, ProjectItem itemElement, string projectPath)
        {
            // The item type must match
            if (!expectedItem.ItemType.Equals(itemElement.ItemType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // The item must have an include attribute
            if (string.IsNullOrEmpty(itemElement.EvaluatedInclude))
            {
                return false;
            }

            // The file name must match
            var fileName = Path.GetFileName(itemElement.EvaluatedInclude);
            if (!fileName.Equals(expectedItem.ItemName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var projectDir = Path.GetDirectoryName(projectPath)!;
            var filePath = Path.IsPathRooted(itemElement.EvaluatedInclude) ?
                itemElement.EvaluatedInclude :
                Path.Combine(projectDir, itemElement.EvaluatedInclude);

            Logger.LogDebug("Considering {FilePath} for expected file {ExpectedFileName}", filePath, expectedItem.ItemName);

            // The included file must exist
            if (!File.Exists(filePath))
            {
                Logger.LogDebug("File {FilePath} does not exist", filePath);
                return false;
            }

            // The file must include all specified keywords
            if (expectedItem.Keywords.Length > 0)
            {
                var fileContents = File.ReadAllText(filePath);
                if (expectedItem.Keywords.Any(k => !fileContents.Contains(k, StringComparison.Ordinal)))
                {
                    Logger.LogDebug("File {FilePath} does not contain all necessary keywords to match", filePath);
                    return false;
                }
            }

            Logger.LogDebug("File {FilePath} matches expected file {ExpectedFileName}", filePath, expectedItem.ItemName);
            return true;
        }

        private void RenameFile(string filePath, ProjectRootElement project)
        {
            var fileName = Path.GetFileName(filePath);
            var backupName = $"{Path.GetFileNameWithoutExtension(fileName)}.old{Path.GetExtension(fileName)}";
            var counter = 0;
            while (File.Exists(backupName))
            {
                backupName = $"{Path.GetFileNameWithoutExtension(fileName)}.old.{counter++}{Path.GetExtension(fileName)}";
            }

            Logger.LogInformation("File already exists, moving {FileName} to {BackupFileName}", fileName, backupName);

            // Even though the file may not make sense in the migrated project,
            // don't remove the file from the project because the user will probably want to migrate some of the code manually later
            // so it's useful to leave it in the project so that the migration need is clearly visible.
            foreach (var item in project.Items.Where(i => i.Include.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                item.Include = backupName;
            }

            foreach (var item in project.Items.Where(i => i.Update.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                item.Update = backupName;
            }

            File.Move(filePath, Path.Combine(Path.GetDirectoryName(filePath)!, backupName));
        }
    }
}
