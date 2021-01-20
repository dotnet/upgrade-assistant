using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildProject : IProject
    {
        private readonly Workspace _ws;
        private readonly ILogger _logger;

        public string FilePath { get; }

        public string Directory => Path.GetDirectoryName(FilePath)!;

        public MSBuildProject(Workspace ws, string path, ILogger logger)
        {
            _ws = ws;
            FilePath = path;
            _logger = logger;
        }

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p =>
        {
            var project = _ws.CurrentSolution.GetProject(p.ProjectId);

            if (project?.FilePath is null)
            {
                throw new InvalidOperationException("Could not find project path for reference");
            }

            return new MSBuildProject(_ws, project.FilePath, _logger);
        });

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
            => GetFile().FindFiles(itemType, matcher);

        public IEnumerable<NuGetReference> PackageReferences
        {
            get
            {
                var packages = GetFile().ProjectRoot.GetAllPackageReferences();

                return packages.Select(p => p.AsNuGetReference()).ToList();
            }
        }

        IProjectFile IProject.GetFile() => GetFile();

        public MSBuildProjectFile GetFile() => new MSBuildProjectFile(this, _logger);

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
            => _ws.CurrentSolution.Projects.First(p => string.Equals(p.FilePath, FilePath, StringComparison.OrdinalIgnoreCase));
    }
}
