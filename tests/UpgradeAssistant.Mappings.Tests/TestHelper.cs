// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

internal static class TestHelper
{
    public static readonly string MappingsDir;

    static TestHelper()
    {
        var location = typeof(TestHelper).Assembly.Location;
        var baseDir = Path.GetDirectoryName(location);

        MappingsDir = Path.GetFullPath(Path.Combine(baseDir!, "mappings"));
    }

    public static string GetRelativePath(string path)
    {
        return Path.GetRelativePath(MappingsDir, path);
    }
}
