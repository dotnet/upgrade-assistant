// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public abstract class CrawlerStore
{
    protected const string ApiCatalogName = "apicatalog.dat";
    protected const string DatabaseName = "usages.db";
    protected const string UsagesName = "usages.tsv";

    public abstract Task DownloadApiCatalogAsync(string fileName);

    public abstract Task<bool> DownloadDatabaseAsync(string fileName);

    public abstract Task UploadDatabaseAsync(string fileName);

    public abstract Task UploadResultsAsync(string fileName);
}
