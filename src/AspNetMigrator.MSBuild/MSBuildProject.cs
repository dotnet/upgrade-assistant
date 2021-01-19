using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildProject : IProject
    {
        private readonly Workspace _ws;
        private readonly ProjectId _projectId;

        public string? FilePath => GetRoslynProject()?.FilePath;

        public string? Directory => FilePath is null ? null : (Path.GetDirectoryName(FilePath) ?? string.Empty);

        public MSBuildProject(Workspace ws, ProjectId projectId)
        {
            _ws = ws;
            _projectId = projectId;
        }

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p => new MSBuildProject(_ws, p.ProjectId));

        public override bool Equals(object? obj)
        {
            if (obj is MSBuildProject other)
            {
                return _projectId.Id.Equals(other._projectId.Id);
            }

            return false;
        }

        public override int GetHashCode() => _projectId.Id.GetHashCode();

        public Project GetRoslynProject() => _ws.CurrentSolution.GetProject(_projectId)!;

        public bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token)
        {
            using var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();

            // Get a Microsoft.Build.EvaluatonProject from the Microsoft.CodeAnalysis.Project
            // provided by the migration context.
            var codeAnalysisProject = GetRoslynProject();
            if (codeAnalysisProject is null || FilePath is null || Directory is null)
            {
                return false;
            }

            var targetItemPath = GetPathRelativeToProject(itemName, Directory);

            var project = projectCollection.LoadProject(FilePath);

            var candidateItems = project.Items.Where(i => GetPathRelativeToProject(i.EvaluatedInclude, Directory).Equals(targetItemPath, StringComparison.OrdinalIgnoreCase));
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
