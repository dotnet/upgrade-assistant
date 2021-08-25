// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class Factories
    {
        private readonly Func<IUpgradeContext, IProject, INuGetReferences> _nugetReferenceFactory;
        private readonly Func<IProjectFile, ITargetFrameworkCollection> _tfmCollectionFactory;

        public Factories(
            Func<IUpgradeContext, IProject, INuGetReferences> nugetReferenceFactory,
            Func<IProjectFile, ITargetFrameworkCollection> tfmCollectionFactory)
        {
            _nugetReferenceFactory = nugetReferenceFactory;
            _tfmCollectionFactory = tfmCollectionFactory;
        }

        public INuGetReferences CreateNuGetReferences(IUpgradeContext context, IProject project) => _nugetReferenceFactory(context, project);

        public ITargetFrameworkCollection CreateTfmCollection(IProjectFile project) => _tfmCollectionFactory(project);
    }
}
