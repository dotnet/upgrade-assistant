// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface INuGetReferences
    {
        NugetPackageFormat PackageReferenceFormat { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        IEnumerable<NuGetReference> GetTransitivePackageReferences(TargetFrameworkMoniker tfm);

        bool IsTransitivelyAvailable(string packageName);

        bool IsTransitiveDependency(NuGetReference nugetReference);
    }
}
