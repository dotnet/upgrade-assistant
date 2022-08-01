// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;

using Microsoft.Cci.Extensions;

using NuGet.Packaging.Core;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public static class PackageCrawler
{
    public static async Task<CrawlerResults> CrawlAsync(NuGetFeed feed, PackageIdentity packageId)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(packageId);

        var crawler = new AssemblyCrawler();
        using var reader = await feed.GetPackageAsync(packageId).ConfigureAwait(false);

        foreach (var packagePath in reader.GetFiles())
        {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var assemblyStream = reader.GetStream(packagePath);
            await using var memoryStream = new MemoryStream();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            await assemblyStream.CopyToAsync(memoryStream).ConfigureAwait(false);

            memoryStream.Position = 0;

            using var env = new HostEnvironment();
            var assembly = env.LoadAssemblyFrom(packagePath, memoryStream);
            if (assembly is null)
            {
                continue;
            }

            crawler.Crawl(assembly);
        }

        return crawler.CreateResults();
    }
}
