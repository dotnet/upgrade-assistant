// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public interface IPackageLoader
    {
        IEnumerable<string> PackageSources { get; }

        Task<PackageArchiveReader?> GetPackageArchiveAsync(NuGetReference packageReference, CancellationToken token, string? cachePath = null);

        Task<IEnumerable<NuGetVersion>> GetNewerVersionsAsync(string packageName, NuGetVersion currentVersion, bool latestMinorAndBuildOnly, CancellationToken token);

        Task<NuGetVersion?> GetLatestVersionAsync(string packageName, bool includePreRelease, string[]? packageSources, CancellationToken token);
    }
}
