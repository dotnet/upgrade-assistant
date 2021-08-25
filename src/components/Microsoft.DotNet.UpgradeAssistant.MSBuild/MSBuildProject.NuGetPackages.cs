// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject
    {
        public INuGetReferences NuGetReferences => _factories.CreateNuGetReferences(Context, this);

        public IEnumerable<NuGetReference> PackageReferences => ProjectRoot.GetAllPackageReferences().Select(p => p.AsNuGetReference());
    }
}
