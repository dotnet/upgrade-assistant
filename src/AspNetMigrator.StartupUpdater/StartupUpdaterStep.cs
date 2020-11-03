using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;

namespace AspNetMigrator.StartupUpdater
{
    /// <summary>
    /// A simple migration step that adds files from templates if they're
    /// not present in the project. Adds files necessary for ASP.NET Core app
    /// startup: Program.cs, Startup.cs, appsettings.json, and appsettings.Development.json.
    /// </summary>
    public class StartupUpdaterStep : MigrationStep
    {
        const string ManifestResourcePrefix = "AspNetMigrator.StartupUpdater.Templates.";
        const string RootNamespacePropertyName = "RootNamespace";
        const string TemplateNamespace = "WebApplication1";
        const int BufferSize = 65536;

        // Files that should be present and text that's expected to be in them
        private static readonly IEnumerable<ItemSpec> ExpectedFiles = new List<ItemSpec>()
        {
            new ItemSpec("Compile", "Program.cs", new[] { "Main", "Microsoft.AspNetCore.Hosting" }),
            new ItemSpec("Compile", "Startup.cs", new[] { "Configure", "ConfigureServices" }),
            new ItemSpec("Content", "appsettings.json", Array.Empty<string>()),
            new ItemSpec("Content", "appsettings.Development.json", Array.Empty<string>())
        };

        private List<ItemSpec> _filesToAdd;

        public StartupUpdaterStep(MigrateOptions options, ILogger logger) : base(options, logger)
        {
            Title = $"Update startup code paths";
            Description = $"Add template Program.cs, Startup.cs, and configuration files to {options.ProjectPath}";
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync()
        {
            try
            {
                var project = ProjectRootElement.Open(Options.ProjectPath);
                project.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

                var rootNamespace = project.Properties.FirstOrDefault(p => p.Name.Equals(RootNamespacePropertyName, StringComparison.Ordinal))?.Value;
                if (rootNamespace is null || rootNamespace.Contains("$"))
                {
                    // If there is no root namespace property, default to the project file name
                    rootNamespace = Path.GetFileNameWithoutExtension(Options.ProjectPath);
                }

                var projectDir = Path.GetDirectoryName(Options.ProjectPath);
                var resourceAssembly = typeof(StartupUpdaterStep).Assembly;

                // For each file in _filesToAdd, add the file and do a simple replacement of the template namespace
                // TODO : It will probably worthwhile to make the templating feature more full-featured
                //        We could prompt users about whether they need different features in their Startup and
                //        include/exclude code based on responses.
                foreach (var file in _filesToAdd)
                {
                    // Get the path where the file will be added
                    var path = Path.Combine(projectDir, file.ItemName);

                    // If the given file already exists, move it
                    if (File.Exists(path))
                    {
                        RenameFile(path, project);
                    }

                    // Copy files and add them to the project
                    using var resourceStream = resourceAssembly.GetManifestResourceStream($"{ManifestResourcePrefix}{file.ItemName}");
                    if (resourceStream is null)
                    {
                        Logger.Fatal("File resource not found for file {FileName}", file.ItemName);
                        return (MigrationStepStatus.Failed, $"File resource not found for file {file.ItemName}");
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

                    // Add the new file to the project
                    project.AddItem(file.ItemType, file.ItemName);

                    Logger.Information("Added {FileName} to the project from template file", file.ItemName);
                }
                Logger.Information("{FileCount} files added", _filesToAdd.Count);

                project.Save();

                return (MigrationStepStatus.Complete, $"{_filesToAdd.Count} expected startup files added");
            }
            catch (InvalidProjectFileException)
            {
                Logger.Fatal("Invalid project: {ProjectPath}", Options.ProjectPath);
                return (MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}");
            }
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync()
        {
            if (!File.Exists(Options.ProjectPath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", Options.ProjectPath);
                return Task.FromResult((MigrationStepStatus.Failed, $"Project file {Options.ProjectPath} not found"));
            }

            try
            {
                var projectRoot = ProjectRootElement.Open(Options.ProjectPath);
                projectRoot.Reload(false); // Reload to make sure we're not seeing an old cached version of the project
                var project = new Project(projectRoot, new Dictionary<string, string>(), null);

                Logger.Verbose("Scanning project for {ExpectedFileCount} expected files", ExpectedFiles.Count());
                _filesToAdd = new List<ItemSpec>(ExpectedFiles.Where(e => !project.Items.Any(i => ItemMatches(e, i))));
                Logger.Information("{FilesNeededCount} expected startup files needed", _filesToAdd.Count);

                if (_filesToAdd.Any())
                {
                    Logger.Verbose("Needed files: {NeededFiles}", string.Join(", ", _filesToAdd));
                    return Task.FromResult((MigrationStepStatus.Incomplete, $"{_filesToAdd.Count} expected startup files needed ({string.Join(", ", _filesToAdd)})"));
                }
                else
                {
                    return Task.FromResult((MigrationStepStatus.Complete, "Expected startup files found"));
                }
            }
            catch (InvalidProjectFileException)
            {
                Logger.Fatal("Invalid project: {ProjectPath}", Options.ProjectPath);
                return Task.FromResult((MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}"));
            }
        }

        /// <summary>
        /// Determines if a given project element matches an item specification
        /// </summary>
        private bool ItemMatches(ItemSpec expectedItem, ProjectItem itemElement)
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

            var projectDir = Path.GetDirectoryName(Options.ProjectPath);
            var filePath = Path.IsPathRooted(itemElement.EvaluatedInclude) ?
                itemElement.EvaluatedInclude :
                Path.Combine(projectDir, itemElement.EvaluatedInclude);

            Logger.Verbose("Considering {FilePath} for expected file {ExpectedFileName}", filePath, expectedItem.ItemName);

            // The included file must exist
            if (!File.Exists(filePath))
            {
                Logger.Verbose("File {FilePath} does not exist", filePath);
                return false;
            }

            // The file must include all specified keywords
            if (!(expectedItem.Keywords is null) && expectedItem.Keywords.Length > 0)
            {
                var fileContents = File.ReadAllText(filePath);
                if (expectedItem.Keywords.Any(k => !fileContents.Contains(k, StringComparison.Ordinal)))
                {
                    Logger.Verbose("File {FilePath} does not contain all necessary keywords to match", filePath);
                    return false;
                }
            }

            Logger.Verbose("File {FilePath} matches expected file {ExpectedFileName}", filePath, expectedItem.ItemName);
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

            Logger.Information("File already exists, moving {FileName} to {BackupFileName}", fileName, backupName);

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

            File.Move(filePath, Path.Combine(Path.GetDirectoryName(filePath), backupName));
        }
    }
}
