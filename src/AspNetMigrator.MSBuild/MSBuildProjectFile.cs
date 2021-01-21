using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildProjectFile : IProjectFile
    {
        private const string DefaultSDK = "Microsoft.NET.Sdk";

        private readonly ILogger _logger;
        private readonly MSBuildProject _project;

        public MSBuildProjectFile(MSBuildProject project, ILogger logger)
        {
            _logger = logger;
            _project = project;
            ProjectRoot = GetProjectRootElement(project.FilePath!);
        }

        public ProjectRootElement ProjectRoot { get; }

        public string TargetSdk
        {
            get
            {
                var sdk = ProjectRoot.Sdk;

                if (sdk is null)
                {
                    throw new ArgumentOutOfRangeException("Should check IsSdk property first");
                }

                return sdk;
            }
        }

        public bool IsSdk =>
            ProjectRoot.Sdk is not null && ProjectRoot.Sdk.Contains(DefaultSDK, StringComparison.OrdinalIgnoreCase);

        public string FilePath => _project.FilePath;

        public void AddPackages(IEnumerable<NuGetReference> references)
        {
            const string PackageReferenceType = "PackageReference";

            // Find a place to add new package references
            var packageReferenceItemGroup = ProjectRoot.ItemGroups.FirstOrDefault(g => g.Items.Any(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase)));
            if (packageReferenceItemGroup is null)
            {
                _logger.LogDebug("Creating a new ItemGroup for package references");
                packageReferenceItemGroup = ProjectRoot.CreateItemGroupElement();
                ProjectRoot.AppendChild(packageReferenceItemGroup);
            }
            else
            {
                _logger.LogDebug("Found ItemGroup for package references");
            }

            foreach (var reference in references)
            {
                ProjectRoot.AddPackageReference(packageReferenceItemGroup, reference);
            }
        }

        public void Simplify()
        {
            // TEMPORARY WORKAROUND
            // https://github.com/dotnet/roslyn/issues/36781
            ProjectRoot.WorkAroundRoslynIssue36781();
        }

        private static ProjectRootElement GetProjectRootElement(string path)
        {
            var projectRoot = ProjectRootElement.Open(path);
            projectRoot.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

            return projectRoot!;
        }

        public ValueTask SaveAsync(CancellationToken token)
        {
            ProjectRoot.Save();

            // Reload the workspace since, at this point, the project may be different from what was loaded
            return _project.Context.ReloadWorkspaceAsync(token);
        }

        public void RemovePackages(IEnumerable<NuGetReference> references)
        {
            foreach (var reference in _project.PackageReferences)
            {
                if (references.Contains(reference))
                {
                    _logger.LogInformation("Removing outdated packaged reference: {PackageReference}", reference);

                    ProjectRoot.RemovePackage(reference);
                }
            }
        }

        public void RenameFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var backupName = $"{Path.GetFileNameWithoutExtension(fileName)}.old{Path.GetExtension(fileName)}";
            var counter = 0;

            while (File.Exists(backupName))
            {
                backupName = $"{Path.GetFileNameWithoutExtension(fileName)}.old.{counter++}{Path.GetExtension(fileName)}";
            }

            _logger.LogInformation("File already exists, moving {FileName} to {BackupFileName}", fileName, backupName);

            // Even though the file may not make sense in the migrated project,
            // don't remove the file from the project because the user will probably want to migrate some of the code manually later
            // so it's useful to leave it in the project so that the migration need is clearly visible.
            foreach (var item in ProjectRoot.Items.Where(i => i.Include.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                item.Include = backupName;
            }

            foreach (var item in ProjectRoot.Items.Where(i => i.Update.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                item.Update = backupName;
            }

            File.Move(filePath, Path.Combine(Path.GetDirectoryName(filePath)!, backupName));
        }

        public void AddItem(string name, string path)
            => ProjectRoot.AddItem(name, path);

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
        {
            using var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(FilePath);
            var items = project.Items
                .Where(i => i.ItemType.Equals(itemType) && matcher.Match(i.EvaluatedInclude));

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(Path.GetDirectoryName(project.FullPath) ?? string.Empty, item.EvaluatedInclude);
            }
        }

        public bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token)
        {
            using var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();

            if (FilePath is null || _project.Directory is null)
            {
                return false;
            }

            var targetItemPath = GetPathRelativeToProject(itemName, _project.Directory);
            var project = projectCollection.LoadProject(FilePath);
            var candidateItems = project.Items.Where(i => GetPathRelativeToProject(i.EvaluatedInclude, _project.Directory).Equals(targetItemPath, StringComparison.OrdinalIgnoreCase));

            if (itemType is not null)
            {
                candidateItems = candidateItems.Where(i => i.ItemType.Equals(itemType.Name, StringComparison.OrdinalIgnoreCase));
            }

            return candidateItems.Any();
        }

        private static string GetPathRelativeToProject(string path, string projectDir) =>
            Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(projectDir, path);
    }
}
