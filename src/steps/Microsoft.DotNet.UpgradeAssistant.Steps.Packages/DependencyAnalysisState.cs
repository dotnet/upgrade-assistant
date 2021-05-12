// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    internal class DependencyAnalysisState : IDependencyAnalysisState
    {
        public DependencyAnalysisState(IProject project, INuGetReferences nugetReferences)
        {
            FrameworkReferences = new DependencyCollection<Reference>(project.FrameworkReferences, SetRisk);
            Packages = new DependencyCollection<NuGetReference>(nugetReferences.PackageReferences, SetRisk);
            References = new DependencyCollection<Reference>(project.References, SetRisk);

            void SetRisk(BuildBreakRisk risk)
            {
                Risk = (BuildBreakRisk)Math.Max((int)Risk, (int)risk);
            }
        }

        public DependencyCollection<Reference> FrameworkReferences { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.FrameworkReferences => FrameworkReferences;

        public DependencyCollection<NuGetReference> Packages { get; }

        IDependencyCollection<NuGetReference> IDependencyAnalysisState.Packages => Packages;

        public DependencyCollection<Reference> References { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.References => References;

        public BuildBreakRisk Risk { get; private set; }

        public bool ChangesRecommended => FrameworkReferences.HasChanges || Packages.HasChanges || References.HasChanges;
    }
}
