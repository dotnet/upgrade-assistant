// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public sealed class FilePackageListCrawler : PackageListCrawler
{
    private readonly string _fileName;

    public FilePackageListCrawler(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        _fileName = fileName;
    }

    public override async Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync()
    {
        var lines = await File.ReadAllLinesAsync(_fileName).ConfigureAwait(false);
        var result = new List<PackageIdentity>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var parts = line.Split('\t');

            if (parts.Length == 2 &&
                NuGetVersion.TryParse(parts[1], out var version))
            {
                var packageId = new PackageIdentity(parts[0], version);
                result.Add(packageId);
            }
            else
            {
                Console.WriteLine($"warning: invalid package format in line {i + 1}: {line}");
            }
        }

        return result.ToArray();
    }
}
