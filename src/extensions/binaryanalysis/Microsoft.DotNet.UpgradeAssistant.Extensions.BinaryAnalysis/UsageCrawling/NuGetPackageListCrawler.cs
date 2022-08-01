// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Packaging.Core;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class NuGetPackageListCrawler : PackageListCrawler
{
    private readonly NuGetFeed _feed;

    public NuGetPackageListCrawler(NuGetFeed feed)
    {
        ArgumentNullException.ThrowIfNull(feed);

        _feed = feed;
    }

    public override Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync()
    {
        var result = _feed.GetAllPackages();

        // The NuGet package list crawler allocates a ton of memory because it fans out pretty hard.
        // Let's make sure we're releasing as much memory as can so that the processes we're about
        // to spin up got more memory to play with.
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        return result;
    }
}
