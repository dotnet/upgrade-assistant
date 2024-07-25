// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

public partial class ValidationTests
{
    private static readonly string[] Kinds = new string[] { "property", "method", "namespace", "type", "xmlnamespace" };
    private static readonly string[] States = new string[] { "NotImplemented", "Removed", "Replaced" };

    [TestMethod]
    public void ValidateApiMaps()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var jsonFiles = Directory.GetFiles(TestHelper.MappingsDir, "*.json", SearchOption.AllDirectories);

        foreach (var path in jsonFiles)
        {
            var fileName = Path.GetFileName(path);
            
            if (fileName.Equals("apimap.json", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".apimap.json", StringComparison.OrdinalIgnoreCase))
            {
                AssertApiMap(options, path);
            }
        }
    }

    private static void AssertApiMap(JsonSerializerOptions options, string fullPath)
    {
        var relativePath = TestHelper.GetRelativePath(fullPath);
        Dictionary<string, ApiMapEntry> mappings;

        try
        {
            var json = File.ReadAllText(fullPath);
            mappings = JsonSerializer.Deserialize<Dictionary<string, ApiMapEntry>>(json, options)
                ?? new Dictionary<string, ApiMapEntry>();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to deserialize {relativePath}: {ex}");
            return;
        }

        foreach (var mapping in mappings)
        {
            var entry = mapping.Value;

            Assert.IsNotNull(entry.Kind, $"`{relativePath}' - [\"{mapping.Key}\"][\"kind\"] cannot be null.");
            Assert.IsTrue(Kinds.Contains(entry.Kind.ToLowerInvariant()), $"`{relativePath}' - [\"{mapping.Key}\"][\"kind\"] must be one of: {string.Join(", ", Kinds)}");

            Assert.IsNotNull(entry.State, $"`{relativePath}' - [\"{mapping.Key}\"][\"state\"] cannot be null.");
            Assert.IsTrue(States.Contains(entry.State), $"`{relativePath}' - [\"{mapping.Key}\"][\"state\"] must be one of: {string.Join(", ", States)}");

            if (entry.Value != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entry.Value), $"`{relativePath}' - [\"{mapping.Key}\"][\"value\"] cannot be empty.");
            }
        }
    }
}
