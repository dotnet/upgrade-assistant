using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IProject
    {
        Project GetRoslynProject();

        IEnumerable<IProject> ProjectReferences { get; }
    }
}
