// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageRestorer
    {
        /// <summary>
        /// Restores NuGet packages for a project.
        /// </summary>
        /// <param name="context">The upgrade context to restore NuGet packages for.</param>
        /// <returns>Whether restore was successfull.</returns>
        Task<bool> RestorePackagesAsync(IUpgradeContext context, IProject project, CancellationToken token);
    }
}
