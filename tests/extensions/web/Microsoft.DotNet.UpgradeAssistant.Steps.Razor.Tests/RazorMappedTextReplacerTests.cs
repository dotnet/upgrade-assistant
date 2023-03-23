// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac.Extras.Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public sealed class RazorMappedTextReplacerTests : IDisposable
    {
        private static readonly string WorkingDir = Path.Combine(Path.GetTempPath(), "RazorTextReplacerTestFiles");

        public RazorMappedTextReplacerTests()
        {
            if (Directory.Exists(WorkingDir))
            {
                Directory.Delete(WorkingDir, true);
            }

            Directory.CreateDirectory(WorkingDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(WorkingDir))
            {
                Directory.Delete(WorkingDir, true);
            }
        }

        [Fact]
        public void CtorNegativeTests()
        {
            Assert.Throws<ArgumentNullException>("logger", () => new RazorMappedTextReplacer(null!));
        }

        [Fact]
        public void ApplyTextReplacementsNegativeTests()
        {
            using var mock = AutoMock.GetLoose();
            var replacer = mock.Create<RazorMappedTextReplacer>();

            Assert.Throws<ArgumentNullException>("replacements", () => replacer.ApplyTextReplacements(null!));
        }

        [Theory]
        [MemberData(nameof(ApplyReplacementsData))]
        public void ApplyTextReplacementsPositiveTests(string testCaseId, IEnumerable<MappedTextReplacement> replacements)
        {
            // Arrange
            replacements = StageInputFiles(replacements);
            var inputFiles = replacements.Select(r => r.FilePath).Distinct().OrderBy(p => p);
            var expectedPostReplacementFiles = Directory.GetFiles(Path.Combine("TestViewsAfterReplacement", testCaseId), "*.cshtml").OrderBy(p => p);
            using var mock = AutoMock.GetLoose();
            var replacer = mock.Create<RazorMappedTextReplacer>();

            // Act
            replacer.ApplyTextReplacements(replacements);

            // Assert
            Assert.Collection(inputFiles, expectedPostReplacementFiles.Select<string, Action<string>>(e => a =>
            {
                Assert.Equal(Path.GetFileName(e), Path.GetFileName(a));

                var expectedText = File.ReadAllText(e).ReplaceLineEndings().TrimEnd();
                var actualText = File.ReadAllText(a).ReplaceLineEndings().TrimEnd();

                Assert.Equal(expectedText, actualText);
            }).ToArray());
        }

        public static IEnumerable<object[]> ApplyReplacementsData =>
            new List<object[]>
            {
                // No replacements
                new object[]
                {
                    "NoReplacements",
                    Enumerable.Empty<MappedTextReplacement>()
                },

                // Vanilla replacements
                new object[]
                {
                    "VanillaReplacements",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("ToString()", "ToAnotherString()", GetPath("Simple.cshtml"), 1),
                        new MappedTextReplacement("Model[1]", "Model[2]", GetPath("View.cshtml"), 18),
                        new MappedTextReplacement("if(Model != null && Model.Length > 1)", "if(Model is not null)", GetPath("View.cshtml"), 15)
                    }
                },

                // Multi-line replacement
                new object[]
                {
                    "MultilineReplacement",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement(
                            "{\r\n    <div>\r\n        <p>@Model[1]</p>\r\n    </div>\r\n}\r\n".ReplaceLineEndings(),
                            "<h1>\r\n    Hi!\r\n</h1>".ReplaceLineEndings(),
                            GetPath("View.cshtml"),
                            16)
                    }
                },

                // Inapplicable replacement
                new object[]
                {
                    "InapplicableReplacement",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("DateTime", "DateTimeOffset", GetPath("Simple.cshtml"), 2),
                        new MappedTextReplacement("<div>\r\n ".ReplaceLineEndings(), "<div>\r\n".ReplaceLineEndings(), GetPath("View.cshtml"), 9),
                    }
                },

                // Adding code to start-of-file
                new object[]
                {
                    "NewTextAtStart",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Something inapplicable", "using Foo;\r\nSomething inapplicable".ReplaceLineEndings(), GetPath("Simple.cshtml"), 0),
                        new MappedTextReplacement(string.Empty, "Test", GetPath("View.cshtml"), 0),
                    }
                },

                // Remove code block
                new object[]
                {
                    "RemoveCodeBlock",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("DateTime.Now.ToString();", string.Empty, GetPath("Simple.cshtml"), 1),
                        new MappedTextReplacement("using Foo;", string.Empty, GetPath("View2.cshtml"), 1),
                        new MappedTextReplacement("Model[0]", string.Empty, GetPath("View2.cshtml"), 7),
                        new MappedTextReplacement("\r\n        var x = 0;\r\n".ReplaceLineEndings(), string.Empty, GetPath("View2.cshtml"), 23),
                    }
                },

                // Remove partial code block
                new object[]
                {
                    "RemovePartialCodeBlock",
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement(".ToString();", ";", GetPath("Simple.cshtml"), 1),
                        new MappedTextReplacement(" Foo", string.Empty, GetPath("View2.cshtml"), 1),
                        new MappedTextReplacement("Model[0];", "Model;", GetPath("View2.cshtml"), 7),
                    }
                }
            };

        private static string GetPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestViews", fileName);

        private static IEnumerable<MappedTextReplacement> StageInputFiles(IEnumerable<MappedTextReplacement> replacements)
        {
            var inputFiles = replacements.Select(r => r.FilePath).Distinct();

            foreach (var inputFile in inputFiles)
            {
                File.Copy(inputFile, Path.Combine(WorkingDir, Path.GetFileName(inputFile)), true);
            }

            return replacements.Select(r => new MappedTextReplacement(r.OriginalText, r.NewText, Path.Combine(WorkingDir, Path.GetFileName(r.FilePath)), r.StartingLine));
        }
    }
}
