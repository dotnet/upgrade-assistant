using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IProject
    {
        string? Directory { get; }

        string? FilePath { get; }

        Project GetRoslynProject();

        IEnumerable<IProject> ProjectReferences { get; }

        bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token);
    }
}
