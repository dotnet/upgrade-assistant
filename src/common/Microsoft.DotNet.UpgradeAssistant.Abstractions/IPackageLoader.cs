// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageLoader
    {
        Task<bool> DoesPackageSupportTargetFrameworksAsync(NuGetReference packageReference, IEnumerable<TargetFrameworkMoniker> targetFrameworks, CancellationToken token);

        IAsyncEnumerable<NuGetReference> GetNewerVersionsAsync(NuGetReference reference, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, CancellationToken token);

        Task<NuGetReference?> GetLatestVersionAsync(string packageName, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, CancellationToken token);

        Task<NuGetPackageMetadata?> GetPackageMetadata(NuGetReference reference, CancellationToken token);
    }
}
