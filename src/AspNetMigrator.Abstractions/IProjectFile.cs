using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface IProjectFile
    {
        string TargetSdk { get; }

        public bool IsSdk { get; }

        string FilePath { get; }

        void AddPackages(IEnumerable<NuGetReference> references);

        void RemovePackages(IEnumerable<NuGetReference> referenceItem);

        ValueTask SaveAsync(CancellationToken token);

        void Simplify();

        void RenameFile(string filePath);

        void AddItem(string name, string path);

        bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token);
    }
}
