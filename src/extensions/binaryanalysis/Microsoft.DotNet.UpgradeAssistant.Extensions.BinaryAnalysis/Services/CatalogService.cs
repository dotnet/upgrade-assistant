// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;

using Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis
{
    public static class CatalogService
    {
        public static async Task<ApiCatalogModel> LoadCatalogAsync()
        {
            var catalogPath = GetCatalogPath();

            if (!File.Exists(catalogPath))
            {
                DownloadCatalog();
            }

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                return await ApiCatalogModel.LoadAsync(catalogPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"error: can't open catalog: {ex.Message}").ConfigureAwait(false);
                Environment.Exit(1);
                return null;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private static string GetCatalogPath()
        {
            var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
            var catalogPath = Path.Join(processDirectory, "apicatalog.db");
            return catalogPath;
        }

        public static void DownloadCatalog(bool force = false)
        {
            var catalogPath = GetCatalogPath();

            var url = ApiCatalogModel.Url;
            var blobClient = new BlobClient(url);

            if (!force && File.Exists(catalogPath))
            {
                Console.WriteLine("Checking catalog...");
                var localTimetamp = File.GetLastWriteTimeUtc(catalogPath);
                var properties = blobClient.GetProperties();
                var blobTimestamp = properties.Value.LastModified.UtcDateTime;
                var blobIsNewer = blobTimestamp > localTimetamp;

                if (!blobIsNewer)
                {
                    Console.WriteLine("Catalog is up-to-date.");
                    return;
                }
            }

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                Console.WriteLine("Downloading catalog...");
                blobClient.DownloadTo(catalogPath);
                var properties = blobClient.GetProperties();
                File.SetLastWriteTimeUtc(catalogPath, properties.Value.LastModified.UtcDateTime);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: can't download catalog: {ex.Message}");
                Environment.Exit(1);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
