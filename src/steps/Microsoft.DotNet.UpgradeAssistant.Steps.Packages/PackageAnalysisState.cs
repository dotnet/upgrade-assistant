// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Packages;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    internal class PackageAnalysisState : IDependencyAnalysisState
    {
        public PackageAnalysisState(IProject project, INuGetReferences nugetReferences)
        {
            FrameworkReferences = new PackageCollection<Reference>(project.FrameworkReferences, SetRisk);
            Packages = new PackageCollection<NuGetReference>(nugetReferences.PackageReferences, SetRisk);
            References = new PackageCollection<Reference>(project.References, SetRisk);

            void SetRisk(BuildBreakRisk risk)
            {
                if (risk != BuildBreakRisk.None)
                {
                    PossibleBreakingChangeRecommended = true;
                }
            }
        }

        public PackageCollection<Reference> FrameworkReferences { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.FrameworkReferences => FrameworkReferences;

        public PackageCollection<NuGetReference> Packages { get; }

        IDependencyCollection<NuGetReference> IDependencyAnalysisState.Packages => Packages;

        public PackageCollection<Reference> References { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.References => References;

        public bool PossibleBreakingChangeRecommended { get; private set; }

        public bool ChangesRecommended => FrameworkReferences.HasChanges || Packages.HasChanges || References.HasChanges;
    }
}
