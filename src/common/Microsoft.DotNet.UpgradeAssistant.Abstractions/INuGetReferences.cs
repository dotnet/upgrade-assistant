// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface INuGetReferences
    {
        NugetPackageFormat PackageReferenceFormat { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        IAsyncEnumerable<NuGetReference> GetTransitivePackageReferencesAsync(TargetFrameworkMoniker tfm, CancellationToken token);

        ValueTask<bool> IsTransitivelyAvailableAsync(string packageName, CancellationToken token);

        ValueTask<bool> IsTransitiveDependencyAsync(NuGetReference nugetReference, CancellationToken token);
    }
}
