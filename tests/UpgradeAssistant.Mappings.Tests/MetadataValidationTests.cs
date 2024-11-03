// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

public partial class ValidationTests
{
    [TestMethod]
    public void ValidateMetadataFiles()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var jsonFiles = Directory.GetFiles(TestHelper.MappingsDir, "metadata.json", SearchOption.AllDirectories);

        foreach (var path in jsonFiles)
        {
            AssertMetadataFile(options, path);
        }
    }

    private static void AssertMetadataFile(JsonSerializerOptions options, string fullPath)
    {
        var relativePath = TestHelper.GetRelativePath(fullPath);
        var json = File.ReadAllText(fullPath);
        JsonDocument doc;

        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = options.AllowTrailingCommas,
                CommentHandling = options.ReadCommentHandling,
                MaxDepth = options.MaxDepth
            });
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to parse {relativePath}: {ex.Message}");
            return;
        }

        var root = doc.RootElement;

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("traits"))
            {
                AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.String);
                var traits = property.Value.GetString();

                if (traits != null)
                {
                    try
                    {
                        TraitsExpressionParser.Validate(traits);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(ex.Message);
                    }
                }
            }
            else if (property.NameEquals("order"))
            {
                AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.Number);
                Assert.IsTrue(property.Value.TryGetInt32(out var order), $"Failed to parse \"{property.Name}\" property in `{relativePath}': {property}");
                Assert.IsTrue(order >= 0, $"`{relativePath}' - [\"{property.Name}\"] must be greater than or equal to 0.");
            }
            else
            {
                AssertUnknownProperty(relativePath, string.Empty, property);
            }
        }
    }
}
