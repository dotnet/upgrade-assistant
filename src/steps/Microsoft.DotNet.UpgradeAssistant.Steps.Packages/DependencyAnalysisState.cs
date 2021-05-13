// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public sealed class DependencyAnalysisState : IDependencyAnalysisState
    {
        public DependencyAnalysisState(IProject project, INuGetReferences nugetReferences)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (nugetReferences is null)
            {
                throw new ArgumentNullException(nameof(nugetReferences));
            }

            FrameworkReferences = new DependencyCollection<Reference>(initial: project.FrameworkReferences, setRisk: SetRisk);
            Packages = new DependencyCollection<NuGetReference>(initial: nugetReferences.PackageReferences, setRisk: SetRisk);
            References = new DependencyCollection<Reference>(project.References, SetRisk);
            IsValid = true;
            void SetRisk(BuildBreakRisk risk)
            {
                Risk = (BuildBreakRisk)Math.Max((int)Risk, (int)risk);
            }
        }

        internal DependencyCollection<Reference> FrameworkReferences { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.FrameworkReferences => FrameworkReferences;

        internal DependencyCollection<NuGetReference> Packages { get; }

        IDependencyCollection<NuGetReference> IDependencyAnalysisState.Packages => Packages;

        internal DependencyCollection<Reference> References { get; }

        IDependencyCollection<Reference> IDependencyAnalysisState.References => References;

        public BuildBreakRisk Risk { get; set; }

        public bool AreChangesRecommended => FrameworkReferences.HasChanges || Packages.HasChanges || References.HasChanges;

        public bool IsValid { get; set; }
    }
}
