// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageLoader
    {
        IEnumerable<string> PackageSources { get; }

        Task<bool> DoesPackageSupportTargetFrameworkAsync(NuGetReference packageReference, string cachePath, TargetFrameworkMoniker targetFramework, CancellationToken token);

        Task<IEnumerable<NuGetReference>> GetNewerVersionsAsync(NuGetReference reference, bool latestMinorAndBuildOnly, CancellationToken token);

        Task<NuGetReference?> GetLatestVersionAsync(string packageName, bool includePreRelease, string[]? packageSources, CancellationToken token);
    }
}
