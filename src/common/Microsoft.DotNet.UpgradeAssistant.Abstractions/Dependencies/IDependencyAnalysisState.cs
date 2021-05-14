// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface IDependencyAnalysisState
    {
        IDependencyCollection<Reference> FrameworkReferences { get; }

        IDependencyCollection<NuGetReference> Packages { get; }

        IDependencyCollection<Reference> References { get; }

        bool IsValid { get; }

        bool AreChangesRecommended { get; }

        BuildBreakRisk Risk { get; }
    }
}
