using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant
{
    public interface IProjectFile
    {
        string Sdk { get; }

        public bool IsSdk { get; }

        string FilePath { get; }

        void AddPackages(IEnumerable<NuGetReference> references);

        void RemovePackages(IEnumerable<NuGetReference> referenceItem);

        ValueTask SaveAsync(CancellationToken token);

        void Simplify();

        void RenameFile(string filePath);

        void AddItem(string name, string path);

        bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token);

        string GetPropertyValue(string propertyName);
    }
}
