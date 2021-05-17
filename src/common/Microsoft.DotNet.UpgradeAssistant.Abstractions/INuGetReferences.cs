// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface INuGetReferences
    {
        /// <summary>
        /// Gets the NuGet package format used in the project file (ie <c>packages.config</c> or <c>PackageReference</c> nodes).
        /// </summary>
        NugetPackageFormat PackageReferenceFormat { get; }

        /// <summary>
        /// Gets the collection of packages that are directly referenced by the project.
        /// </summary>
        IEnumerable<NuGetReference> PackageReferences { get; }

        /// <summary>
        /// Gets the packages that are transitively referenced via other packages or projects for the given <paramref name="tfm"/>.
        /// </summary>
        /// <param name="tfm">The target framework to get the dependencies for.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A collection of transitive dependencies.</returns>
        IAsyncEnumerable<NuGetReference> GetTransitivePackageReferencesAsync(TargetFrameworkMoniker tfm, CancellationToken token);

        /// <summary>
        /// Checks if the package name is transitively referenced via other packages or projects.
        /// </summary>
        /// <param name="packageName">The name of a package.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns whether the package is transitively referenced.</returns>
        ValueTask<bool> IsTransitivelyAvailableAsync(string packageName, CancellationToken token);

        /// <summary>
        /// Checks if the package is transitively referenced via other packages or projects.
        /// </summary>
        /// <param name="nugetReference">The package identity.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns whether the package is transitively referenced.</returns>
        ValueTask<bool> IsTransitiveDependencyAsync(NuGetReference nugetReference, CancellationToken token);
    }
}
