// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

public partial class ValidationTests
{
    [TestMethod]
    public void ValidatePackageMaps()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var jsonFiles = Directory.GetFiles(TestHelper.MappingsDir, "*.json", SearchOption.AllDirectories);

        foreach (var path in jsonFiles)
        {
            var fileName = Path.GetFileName(path);

            if (fileName.Equals("packagemap.json", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".packagemap.json", StringComparison.OrdinalIgnoreCase))
            {
                AssertPackageMap(options, path);
            }
        }
    }

    private static void AssertPackageMapEntry(string relativePath, string packagePath, PackageMapEntry package)
    {
        if (package.Frameworks is not null)
        {
            foreach (var framework in package.Frameworks)
            {
                var count = framework.Value.Count;
                int index = 0;

                foreach (var frameworkEntry in framework.Value)
                {
                    var frameworkEntryPath = $"{packagePath}[\"frameworks\"][{index++}]";

                    if (count > 1 && string.IsNullOrEmpty(frameworkEntry.Name))
                    {
                        // if there are more than one packages to be added instead of old, their names should be specified.
                        Assert.Fail($"`{relativePath}' - {frameworkEntryPath}[\"name\"] cannot be empty if there are more than 1 packages to be added.");
                        break;
                    }

                    if (frameworkEntry.Version is not null && frameworkEntry.Version.Contains('*') && !frameworkEntry.Version.EndsWith('*'))
                    {
                        // if version has * it should be at the end
                        Assert.Fail($"`{relativePath}' - {frameworkEntryPath}[\"version\"] is invalid. Wildcards may only be used at the end of the version string.");
                        break;
                    }
                }
            }
        }
    }

    private static void AssertPackageMap(JsonSerializerOptions options, string fullPath)
    {
        var relativePath = TestHelper.GetRelativePath(fullPath);
        PackageMapConfig config;

        try
        {
            var json = File.ReadAllText(fullPath);
            config = JsonSerializer.Deserialize<PackageMapConfig>(json, options)
                ?? new PackageMapConfig();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to deserialize {TestHelper.GetRelativePath(fullPath)}: {ex}");
            return;
        }

        if (config.Defaults != null)
        {
            AssertPackageMapEntry(relativePath, "[\"defaults\"]", config.Defaults);
        }

        if (config.Packages != null)
        {
            int index = 0;

            foreach (var package in config.Packages)
            {
                var packagePath = $"[\"packages\"][\"{index++}\"]";

                AssertPackageMapEntry(relativePath, packagePath, package);
            }
        }
    }
}
