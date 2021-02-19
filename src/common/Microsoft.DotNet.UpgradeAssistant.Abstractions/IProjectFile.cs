// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
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

        void SetTFM(TargetFrameworkMoniker targetTFM);
    }
}
