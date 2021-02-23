// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageRestorer
    {
        /// <summary>
        /// Restores NuGet packages for a project and returns the location of
        /// the resulting lock file and package cache.
        /// </summary>
        /// <param name="context">The migration context to restore NuGet packages for.</param>
        /// <returns>A RestoreOutput object with the path to the project's lock file
        /// after restoring packages and the location of the NuGet package cache used during restore.</returns>
        Task<RestoreOutput> RestorePackagesAsync(IUpgradeContext context, IProject project, CancellationToken token);
    }
}
