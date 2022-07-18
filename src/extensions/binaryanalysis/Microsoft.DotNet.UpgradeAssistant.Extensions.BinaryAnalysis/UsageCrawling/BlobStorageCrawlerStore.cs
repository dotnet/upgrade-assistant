// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Storage.Blobs;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class BlobStorageCrawlerStore : CrawlerStore
{
    private const string CatalogContainerName = "catalog";
    private const string UsageContainerName = "usage";

    private readonly string _blobStorageConnectionString;

    public BlobStorageCrawlerStore(string blobStorageConnectionString)
    {
        _blobStorageConnectionString = blobStorageConnectionString;
    }

    private async Task EnsureContainerExist()
    {
        var client = new BlobContainerClient(_blobStorageConnectionString, UsageContainerName, GetBlobOptions());
        await client.CreateIfNotExistsAsync().ConfigureAwait(false);
    }

    public override Task DownloadApiCatalogAsync(string fileName)
    {
        var blobClient = new BlobClient(_blobStorageConnectionString, CatalogContainerName, ApiCatalogName, GetBlobOptions());
        return blobClient.DownloadToAsync(fileName);
    }

    public override async Task<bool> DownloadDatabaseAsync(string fileName)
    {
        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        if (!await blobClient.ExistsAsync().ConfigureAwait(false))
        {
            return false;
        }

        await blobClient.DownloadToAsync(fileName).ConfigureAwait(false);
        return true;
    }

    public override async Task UploadDatabaseAsync(string fileName)
    {
        await EnsureContainerExist().ConfigureAwait(false);

        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, DatabaseName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true).ConfigureAwait(false);
    }

    public override async Task UploadResultsAsync(string fileName)
    {
        await EnsureContainerExist().ConfigureAwait(false);

        var blobClient = new BlobClient(_blobStorageConnectionString, UsageContainerName, UsagesName, GetBlobOptions());
        await blobClient.UploadAsync(fileName, overwrite: true).ConfigureAwait(false);
    }

    private static BlobClientOptions GetBlobOptions()
    {
        return new BlobClientOptions
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(90),
                MaxRetries = 10,
                NetworkTimeout = TimeSpan.FromMinutes(5),
            }
        };
    }
}
