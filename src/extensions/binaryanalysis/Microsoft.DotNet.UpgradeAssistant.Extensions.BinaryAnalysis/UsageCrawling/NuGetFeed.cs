// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;

using Newtonsoft.Json;

using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class NuGetFeed
{
    public static NuGetFeed NuGetOrg { get; } = new(new("https://api.nuget.org/v3/index.json"));

    public NuGetFeed(Uri feedUrl)
    {
        ArgumentNullException.ThrowIfNull(feedUrl);

        FeedUrl = feedUrl;
    }

    public Uri FeedUrl { get; }

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackages(DateTimeOffset? since = null)
    {
        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl.OriginalString);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>().ConfigureAwait(false);
        var catalogIndexUrlString = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrlString == null)
        {
            throw new InvalidOperationException("This feed doesn't support enumeration");
        }

        var catalogIndexUrl = new Uri(catalogIndexUrlString);

        const int MaxDegreeOfParallelism = 64;

        ThreadPool.SetMinThreads(MaxDegreeOfParallelism, completionPortThreads: 4);
        ServicePointManager.DefaultConnectionLimit = MaxDegreeOfParallelism;
        ServicePointManager.MaxServicePointIdleTime = 10000;

        using var handler = new HttpClientHandler
        {
            CheckCertificateRevocationList = true,
            SslProtocols = System.Security.Authentication.SslProtocols.None
        };
        using var httpClient = new HttpClient(handler);

        var indexString = await httpClient.GetStringAsync(catalogIndexUrl).ConfigureAwait(false);
        var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString)!;

        // Find all pages in the catalog index.
        var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
        var catalogLeaves = new ConcurrentBag<CatalogLeaf>();

        var fetchLeafsTasks = RunInParallel(async () =>
        {
            while (pageItems.TryTake(out var pageItem))
            {
                if (since != null && pageItem.CommitTimeStamp < since.Value)
                {
                    continue;
                }

                var retryCount = 3;
Retry:
                try
                {
                    // Download the catalog page and deserialize it.
                    var pageString = await httpClient.GetStringAsync(pageItem.Url).ConfigureAwait(false);
                    var page = JsonConvert.DeserializeObject<CatalogPage>(pageString)!;

                    foreach (var pageLeafItem in page.Items)
                    {
                        if (pageLeafItem.Type == "nuget:PackageDetails")
                        {
                            catalogLeaves.Add(pageLeafItem);
                        }
                    }
                }
                catch (Exception ex) when (retryCount > 0)
                {
                    retryCount--;
                    Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
                    goto Retry;
                }
            }
        });

        await Task.WhenAll(fetchLeafsTasks).ConfigureAwait(false);

        return catalogLeaves
            .Select(l => new PackageIdentity(l.Id, NuGetVersion.Parse(l.Version)))
            .Distinct()
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version)
            .ToArray();

        static List<Task> RunInParallel(Func<Task> work)
        {
            return Enumerable.Range(0, MaxDegreeOfParallelism)
                .Select(_ => work())
                .ToList();
        }
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var url = new Uri(await GetPackageUrlAsync(identity).ConfigureAwait(false));

        using var httpClient = new HttpClient();
        var nupkgStream = await httpClient.GetStreamAsync(url).ConfigureAwait(false);
        return new PackageArchiveReader(nupkgStream);
    }

    private async Task<string> GetPackageUrlAsync(PackageIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var sourceRepository = Repository.Factory.GetCoreV3(FeedUrl.OriginalString);
        var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>().ConfigureAwait(false);
        var packageBaseAddress = serviceIndex.GetServiceEntryUri("PackageBaseAddress/3.0.0")?.ToString();

        var id = identity.Id.ToLowerInvariant();
        var version = identity.Version.ToNormalizedString().ToLowerInvariant();
        return $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg";
    }

#nullable disable

    private abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public Uri Url { get; set; }

        [JsonProperty("commitTimeStamp")]
        public DateTime CommitTimeStamp { get; set; }
    }

    private sealed class CatalogIndex : CatalogEntity
    {
        public List<CatalogPage> Items { get; set; }
    }

    private sealed class CatalogPage : CatalogEntity
    {
        public List<CatalogLeaf> Items { get; set; }
    }

    private sealed class CatalogLeaf : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public string Id { get; set; }

        [JsonProperty("nuget:version")]
        public string Version { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }
    }
}
