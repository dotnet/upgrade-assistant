using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using Build = Microsoft.Build.Evaluation;

namespace AspNetMigrator.MSBuild
{
    internal partial class MSBuildProject : IProject
    {
        private readonly ILogger _logger;

        public MSBuildWorkspaceMigrationContext Context { get; }

        public string FilePath { get; }

        public string Directory => Path.GetDirectoryName(FilePath)!;

        public MSBuildProject(MSBuildWorkspaceMigrationContext context, string path, ILogger logger)
        {
            FilePath = path;
            Context = context;
            _logger = logger;
        }

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p =>
        {
            var project = Context.Workspace.CurrentSolution.GetProject(p.ProjectId);

            if (project?.FilePath is null)
            {
                throw new InvalidOperationException("Could not find project path for reference");
            }

            return Context.GetOrAddProject(project.FilePath);
        });

        public Build.Project Project => Context.ProjectCollection.LoadProject(FilePath);

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
        {
            var items = Project.Items
                .Where<Build.ProjectItem>(i => i.ItemType.Equals(itemType.Name) && matcher.Match(i.EvaluatedInclude));

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(Path.GetDirectoryName(FilePath) ?? string.Empty, item.EvaluatedInclude);
            }
        }

        public IEnumerable<NuGetReference> PackageReferences
        {
            get
            {
                var packages = ProjectRoot.GetAllPackageReferences();

                return packages.Select(p => p.AsNuGetReference()).ToList();
            }
        }

        IProjectFile IProject.GetFile() => this;

        public override bool Equals(object? obj)
        {
            if (obj is MSBuildProject other)
            {
                return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);

        public Project GetRoslynProject()
            => Context.Workspace.CurrentSolution.Projects.First(p => string.Equals(p.FilePath, FilePath, StringComparison.OrdinalIgnoreCase));
    }
}
