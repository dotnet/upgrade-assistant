using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildProject : IProject
    {
        private readonly Workspace _ws;
        private readonly ProjectId _projectId;

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
    }
}
