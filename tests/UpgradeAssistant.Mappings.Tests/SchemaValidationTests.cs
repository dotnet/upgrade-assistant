// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

[TestClass]
public partial class ValidationTests
{
    [TestMethod]
    public void ValidateSchemas()
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
                AssertPackageMapSchema(options, path);
            }
            else if (fileName.Equals("apimap.json", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".apimap.json", StringComparison.OrdinalIgnoreCase))
            {
                AssertApiMapSchema(options, path);
            }
            else if (fileName.Equals("metadata.json", StringComparison.OrdinalIgnoreCase))
            {
                AssertConfigSchema(options, path);
            }
            else
            {
                //Assert.Fail($"Unknown file type: {fileName}");
            }
        }
    }

    private static void AssertElementType(string relativePath, string elementPath, JsonElement element, JsonValueKind expectedKind)
    {
        Assert.AreEqual(expectedKind, element.ValueKind, $"The {elementPath} element in `{relativePath}' is expected to be a {expectedKind.ToString().ToLowerInvariant()}.");
    }

    private static string GetPropertyPath(string elementPath, JsonProperty property)
    {
        return $"{elementPath}[\"{property.Name}\"]";
    }

    private static void AssertPropertyType(string relativePath, string elementPath, JsonProperty property, JsonValueKind expectedKind)
    {
        var propertyPath = GetPropertyPath(elementPath, property);

        Assert.AreEqual(expectedKind, property.Value.ValueKind, $"The {propertyPath} property in `{relativePath}' is expected to be a {expectedKind.ToString().ToLowerInvariant()}.");
    }

    private static void AssertPropertyTypeIsBoolean(string relativePath, string elementPath, JsonProperty property)
    {
        var propertyPath = GetPropertyPath(elementPath, property);

        Assert.IsTrue(property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False, $"The {propertyPath} property in `{relativePath}' is expected to be a boolean.");
    }

    private static void AssertUnknownProperty(string relativePath, string elementPath, JsonProperty property)
    {
        Assert.Fail($"Unknown property in `{relativePath}': {GetPropertyPath(elementPath, property)}");
    }

    private static void AssertPackageMapEntryFramework(string relativePath, string frameworkPath, JsonElement framework)
    {
        int index = 0;

        foreach (var element in framework.EnumerateArray())
        {
            var elementPath = $"{frameworkPath}[{index++}]";

            AssertElementType(relativePath, elementPath, element, JsonValueKind.Object);
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("name"))
                {
                    AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
                }
                else if (property.NameEquals("version"))
                {
                    AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
                }
                else if (property.NameEquals("prerelease"))
                {
                    AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
                }
                else
                {
                    AssertUnknownProperty(relativePath, elementPath, property);
                }
            }
        }
    }

    private static void AssertPackageMapFrameworks(string relativePath, string elementPath, JsonElement frameworks)
    {
        foreach (var framework in frameworks.EnumerateObject())
        {
            var frameworkPath = GetPropertyPath(elementPath, framework);
            AssertPropertyType(relativePath, frameworkPath, framework, JsonValueKind.Array);
            AssertPackageMapEntryFramework(relativePath, frameworkPath, framework.Value);
        }
    }

    private static void AssertPackageMapEntry(string relativePath, string elementPath, JsonElement defaults)
    {
        foreach (var property in defaults.EnumerateObject())
        {
            if (property.NameEquals("name"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else if (property.NameEquals("frameworks"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.Object);
                AssertPackageMapFrameworks(relativePath, GetPropertyPath(elementPath, property), property.Value);
            }
            else
            {
                AssertUnknownProperty(relativePath, elementPath, property);
            }
        }
    }

    private static void AssertPackageMapPackages(string relativePath, string packagesPath, JsonElement packages)
    {
        int index = 0;

        foreach (var element in packages.EnumerateArray())
        {
            var elementPath = $"{packagesPath}[{index++}]";
            AssertElementType(relativePath, elementPath, element, JsonValueKind.Object);
            AssertPackageMapEntry(relativePath, elementPath, element);
        }
    }

    private static void AssertPackageMapSchema(JsonSerializerOptions options, string fullPath)
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
            if (property.NameEquals("defaults"))
            {
                AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.Object);
                AssertPackageMapEntry(relativePath, GetPropertyPath(string.Empty, property), property.Value);
            }
            else if (property.NameEquals("packages"))
            {
                AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.Array);
                AssertPackageMapPackages(relativePath, GetPropertyPath(string.Empty, property), property.Value);
            }
            else
            {
                AssertUnknownProperty(relativePath, string.Empty, property);
            }
        }
    }

    private static void AssertApiMapEntry(string relativePath, string elementPath, JsonElement entry)
    {
        foreach (var property in entry.EnumerateObject())
        {
            if (property.NameEquals("value"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else if (property.NameEquals("kind"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else if (property.NameEquals("state"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else if (property.NameEquals("isAsync"))
            {
                AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
            }
            else if (property.NameEquals("isExtension"))
            {
                AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
            }
            else if (property.NameEquals("isStatic"))
            {
                AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
            }
            else if (property.NameEquals("messageId"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else if (property.NameEquals("messageParams"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.Array);

                var propertyPath = GetPropertyPath(elementPath, property);
                int index = 0;

                foreach (var paramElement in property.Value.EnumerateArray())
                {
                    var paramPath = $"{propertyPath}[{index++}]";
                    AssertElementType(relativePath, paramPath, paramElement, JsonValueKind.String);
                }
            }
            else if (property.NameEquals("needsManualUpgrade"))
            {
                AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
            }
            else if (property.NameEquals("needsTodoInComment"))
            {
                AssertPropertyTypeIsBoolean(relativePath, elementPath, property);
            }
            else if (property.NameEquals("documentationUrl"))
            {
                AssertPropertyType(relativePath, elementPath, property, JsonValueKind.String);
            }
            else
            {
                AssertUnknownProperty(relativePath, elementPath, property);
            }
        }
    }

    private static void AssertApiMapSchema(JsonSerializerOptions options, string fullPath)
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
            AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.Object);
            AssertApiMapEntry(relativePath, GetPropertyPath(string.Empty, property), property.Value);
        }
    }

    private static void AssertConfigSchema(JsonSerializerOptions options, string fullPath)
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
            }
            else if (property.NameEquals("order"))
            {
                AssertPropertyType(relativePath, string.Empty, property, JsonValueKind.Number);
            }
            else
            {
                AssertUnknownProperty(relativePath, string.Empty, property);
            }
        }
    }
}
