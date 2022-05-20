// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IProjectFile
    {
        ICollection<string> Sdk { get; }

        public bool IsSdk { get; }

        string FilePath { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        void AddFrameworkReferences(IEnumerable<Reference> frameworkReferences);

        void RemoveFrameworkReferences(IEnumerable<Reference> frameworkReferences);

        void AddPackages(IEnumerable<NuGetReference> packages);

        void RemovePackages(IEnumerable<NuGetReference> packages);

        void RemoveReferences(IEnumerable<Reference> references);

        ValueTask SaveAsync(CancellationToken token);

        void Simplify();

        void RenameFile(string filePath);

        void AddItem(string name, string path);

        public void AddItem(ProjectItemDescriptor projectItem);

        bool RemoveItem(ProjectItemDescriptor projectItem);

        bool ContainsItem(string itemName, ProjectItemType? itemType, CancellationToken token);

        string GetPropertyValue(string propertyName);

        void SetPropertyValue(string propertyName, string propertyValue);

        void RemoveProperty(string propertyName);

        void SetTFM(TargetFrameworkMoniker targetTFM);

        ICollection<string> Imports { get; }
    }
}
