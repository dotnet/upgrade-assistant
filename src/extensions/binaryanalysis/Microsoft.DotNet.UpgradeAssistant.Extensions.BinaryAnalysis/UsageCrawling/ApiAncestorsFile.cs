// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;

public static class ApiAncestorsFile
{
    public static string Name => "api_ancestors.tsv";

    public static IEnumerable<(Guid Api, Guid Ancestor)> Read(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (line.Split('\t') is { Length: 2 } parts &&
                Guid.TryParse(parts[0], out var api) &&
                Guid.TryParse(parts[1], out var ancestor))
            {
                yield return (api, ancestor);
            }
        }
    }
}
