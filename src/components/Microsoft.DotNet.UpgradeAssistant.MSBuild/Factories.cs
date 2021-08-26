// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class Factories
    {
        private readonly Func<IUpgradeContext, IProject, INuGetReferences> _nugetReferenceFactory;
        private readonly Func<IProjectFile, ITargetFrameworkCollection> _tfmCollectionFactory;
        private readonly Func<string, ISolutionInfo> _infoGenerator;

        public Factories(
            Func<IUpgradeContext, IProject, INuGetReferences> nugetReferenceFactory,
            Func<IProjectFile, ITargetFrameworkCollection> tfmCollectionFactory,
            Func<string, ISolutionInfo> infoGenerator)
        {
            _nugetReferenceFactory = nugetReferenceFactory;
            _tfmCollectionFactory = tfmCollectionFactory;
            _infoGenerator = infoGenerator;
        }

        public INuGetReferences CreateNuGetReferences(IUpgradeContext context, IProject project) => _nugetReferenceFactory(context, project);

        public ITargetFrameworkCollection CreateTfmCollection(IProjectFile project) => _tfmCollectionFactory(project);

        public ISolutionInfo CreateSolutionInfo(string name) => _infoGenerator(name);
    }
}
