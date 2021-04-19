// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : IProjectFile
    {
        private ProjectRootElement? _projectRoot;

        string IProjectFile.FilePath => FileInfo.FullName;

        IProjectFile IProject.GetFile() => this;

        // Don't get the project root from Project.Xml since some upgrade
        // steps may need to work with the project XML while it's not in
        // a completely loadable state (for example, prior to converting
        // to a SDK-style  project).
        public ProjectRootElement ProjectRoot
        {
            get
            {
                if (_projectRoot is null)
                {
                    _projectRoot = ProjectRootElement.Open(FileInfo.FullName, Context.ProjectCollection);
                }

                return _projectRoot;
            }
        }

        public string Sdk
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
            ProjectRoot.Sdk is not null && ProjectRoot.Sdk.Contains(MSBuildConstants.DefaultSDK, StringComparison.OrdinalIgnoreCase);

        public IEnumerable<string> Imports => ProjectRoot.Imports.Select(p => Path.GetFileName(p.Project));

        public void SetTFM(TargetFrameworkMoniker tfm) => new TargetFrameworkMonikerCollection(this, _comparer).SetTargetFramework(tfm);

        public void AddPackages(IEnumerable<NuGetReference> references)
        {
            if (references.Any())
            {
                var packageReferenceItemGroup = ProjectRoot.GetOrCreateItemGroup(MSBuildConstants.PackageReferenceType);
                foreach (var reference in references)
                {
                    _logger.LogInformation("Adding package reference: {PackageReference}", reference);
                    ProjectRoot.AddPackageReference(packageReferenceItemGroup, reference);
                }
            }
        }

        public void RemovePackages(IEnumerable<NuGetReference> references)
        {
            foreach (var reference in PackageReferences)
            {
                if (references.Contains(reference))
                {
                    _logger.LogInformation("Removing outdated package reference: {PackageReference}", reference);
                    ProjectRoot.RemovePackage(reference);
                }
            }
        }

        public void AddFrameworkReferences(IEnumerable<Reference> frameworkReferences)
        {
            if (frameworkReferences.Any())
            {
                var frameworkReferenceItemGroup = ProjectRoot.GetOrCreateItemGroup(MSBuildConstants.FrameworkReferenceType);
                foreach (var reference in frameworkReferences)
                {
                    _logger.LogInformation("Adding framework reference: {FrameworkReference}", reference);
                    ProjectRoot.AddFrameworkReference(frameworkReferenceItemGroup, reference);
                }
            }
        }

        public void RemoveFrameworkReferences(IEnumerable<Reference> frameworkReferences)
        {
            foreach (var reference in FrameworkReferences)
            {
                if (frameworkReferences.Contains(reference))
                {
                    _logger.LogInformation("Removing outdated framework reference: {FrameworkReference}", reference);
                    ProjectRoot.RemoveFrameworkReference(reference);
                }
            }
        }

        public void RemoveReferences(IEnumerable<Reference> references)
        {
            foreach (var reference in References)
            {
                if (references.Contains(reference))
                {
                    _logger.LogInformation("Removing outdated assembly reference: {Reference}", reference);
                    ProjectRoot.RemoveReference(reference);
                }
            }
        }

        public void Simplify()
        {
            // TEMPORARY WORKAROUND
            // https://github.com/dotnet/roslyn/issues/36781
            ProjectRoot.WorkAroundRoslynIssue36781();
        }

        public ValueTask SaveAsync(CancellationToken token)
        {
            _logger.LogDebug("Saving changes to project file");
            ProjectRoot.Save();

            // Reload the workspace since, at this point, the project may be different from what was loaded
            return Context.ReloadWorkspaceAsync(token);
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

            // Even though the file may not make sense in the upgraded project,
            // don't remove the file from the project because the user will probably want to upgrade some of the code manually later
            // so it's useful to leave it in the project so that the upgrade need is clearly visible.
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

        public bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token)
        {
            var targetItemPath = GetPathRelativeToProject(itemName, FileInfo.DirectoryName ?? string.Empty);
            var candidateItems = Project.Items
                .Where(i => GetPathRelativeToProject(i.EvaluatedInclude, FileInfo.DirectoryName ?? string.Empty).Equals(targetItemPath, StringComparison.OrdinalIgnoreCase));

            if (itemType is not null)
            {
                candidateItems = candidateItems.Where(i => i.ItemType.Equals(itemType.Name, StringComparison.OrdinalIgnoreCase));
            }

            return candidateItems.Any();
        }

        public string GetPropertyValue(string propertyName)
            => Project.GetPropertyValue(propertyName);

        public void SetPropertyValue(string propertyName, string propertyValue)
            => Project.SetProperty(propertyName, propertyValue);

        private static string GetPathRelativeToProject(string path, string projectDir) =>
            Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(projectDir, path);
    }
}
