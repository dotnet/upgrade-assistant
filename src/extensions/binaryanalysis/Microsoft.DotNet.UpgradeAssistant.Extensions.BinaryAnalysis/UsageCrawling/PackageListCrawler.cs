// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Packaging.Core;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public abstract class PackageListCrawler
{
    public abstract Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync();
}
