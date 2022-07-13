// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class FileSystemCrawlerStore : CrawlerStore
{
    private readonly string _directory;
    private readonly string _outDirectory;

    public FileSystemCrawlerStore(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        _directory = directory;
        _outDirectory = Path.Join(directory, "out");
    }

    private static string GetApiCatalogPath(string directory)
    {
        return Path.Join(directory, ApiCatalogName);
    }

    private static string GetDatabasePath(string directory)
    {
        return Path.Join(directory, DatabaseName);
    }

    private static string GetUsagesPath(string directory)
    {
        return Path.Join(directory, UsagesName);
    }

    public override Task DownloadApiCatalogAsync(string fileName)
    {
        var apiCatalogPath = GetApiCatalogPath(_directory);
        File.Copy(apiCatalogPath, fileName, overwrite: true);
        return Task.CompletedTask;
    }

    public override Task<bool> DownloadDatabaseAsync(string fileName)
    {
        var databasePath = GetDatabasePath(_directory);
        if (!File.Exists(databasePath))
        {
            return Task.FromResult(false);
        }

        File.Copy(databasePath, fileName, overwrite: true);
        return Task.FromResult(true);
    }

    public override Task UploadDatabaseAsync(string fileName)
    {
        var databasePath = GetDatabasePath(_outDirectory);
        var directory = Path.GetDirectoryName(databasePath)!;
        Directory.CreateDirectory(directory);

        File.Copy(fileName, databasePath, overwrite: true);
        return Task.CompletedTask;
    }

    public override Task UploadResultsAsync(string fileName)
    {
        var usagesPath = GetUsagesPath(_outDirectory);
        File.Copy(fileName, usagesPath, overwrite: true);
        return Task.CompletedTask;
    }
}
