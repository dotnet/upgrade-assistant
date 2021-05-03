// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac.Extras.Moq;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public sealed class RazorTextReplacerTests : IDisposable
    {
        private static readonly string WorkingDir = Path.Combine(Path.GetTempPath(), "RazorTextReplacerTestFiles");

        public RazorTextReplacerTests()
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
            Assert.Throws<ArgumentNullException>("logger", () => new RazorTextReplacer(null!));
        }

        [Fact]
        public void ApplyTextReplacementsNegativeTests()
        {
            using var mock = AutoMock.GetLoose();
            var replacer = mock.Create<RazorTextReplacer>();

            Assert.Throws<ArgumentNullException>("replacements", () => replacer.ApplyTextReplacements(null!));
        }

        [Theory]
        [MemberData(nameof(ApplyReplacementsData))]
        public void ApplyTextReplacementsPositiveTests(string testCaseId, IEnumerable<TextReplacement> replacements)
        {
            // Arrange
            replacements = StageInputFiles(replacements);
            var inputFiles = replacements.Select(r => r.FilePath).Distinct().OrderBy(p => p);
            var expectedPostReplacementFiles = Directory.GetFiles(Path.Combine("TestViewsAfterReplacement", testCaseId), "*.cshtml").OrderBy(p => p);
            using var mock = AutoMock.GetLoose();
            var replacer = mock.Create<RazorTextReplacer>();

            // Act
            replacer.ApplyTextReplacements(replacements);

            // Assert
            Assert.Collection(inputFiles, expectedPostReplacementFiles.Select<string, Action<string>>(e => a =>
            {
                Assert.Equal(Path.GetFileName(e), Path.GetFileName(a));
                Assert.Equal(File.ReadAllText(e), File.ReadAllText(a));
            }).ToArray());
        }

        public static IEnumerable<object[]> ApplyReplacementsData =>
            new List<object[]>
            {
                // No replacements
                new object[]
                {
                    "NoReplacements",
                    Enumerable.Empty<TextReplacement>()
                },

                // Vanilla replacements
                new object[]
                {
                    "VanillaReplacements",
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("ToString()"), SourceText.From("ToAnotherString()"), GetPath("Simple.cshtml"), 1),
                        new TextReplacement(SourceText.From("Model[1]"), SourceText.From("Model[2]"), GetPath("View.cshtml"), 18),
                        new TextReplacement(SourceText.From("if(Model != null && Model.Length > 1)"), SourceText.From("if(Model is not null)"), GetPath("View.cshtml"), 15)
                    }
                },

                // Multi-line replacement
                new object[]
                {
                    "MultilineReplacement",
                    new TextReplacement[]
                    {
                        new TextReplacement(
                            SourceText.From("{\r\n    <div>\r\n        <p>@Model[1]</p>\r\n    </div>\r\n}\r\n"),
                            SourceText.From("<h1>\r\n    Hi!\r\n</h1>"),
                            GetPath("View.cshtml"),
                            16)
                    }
                },

                // Inapplicable replacement
                new object[]
                {
                    "InapplicableReplacement",
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("DateTime"), SourceText.From("DateTimeOffset"), GetPath("Simple.cshtml"), 2),
                        new TextReplacement(SourceText.From("<div>\r\n "), SourceText.From("<div>\r\n"), GetPath("View.cshtml"), 9),
                    }
                },

                // Adding code to start-of-file
                new object[]
                {
                    "NewTextAtStart",
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Something inapplicable"), SourceText.From("using Foo;\r\nSomething inapplicable"), GetPath("Simple.cshtml"), 0),
                        new TextReplacement(SourceText.From(string.Empty), SourceText.From("Test"), GetPath("View.cshtml"), 0),
                    }
                },

                // Remove code block
                new object[]
                {
                    "RemoveCodeBlock",
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("DateTime.Now.ToString();"), SourceText.From(string.Empty), GetPath("Simple.cshtml"), 1),
                        new TextReplacement(SourceText.From("using Foo;"), SourceText.From(string.Empty), GetPath("View2.cshtml"), 1),
                        new TextReplacement(SourceText.From("Model[0]"), SourceText.From(string.Empty), GetPath("View2.cshtml"), 7),
                        new TextReplacement(SourceText.From("\r\n        var x = 0;\r\n"), SourceText.From(string.Empty), GetPath("View2.cshtml"), 23),
                    }
                },

                // Remove partial code block
                new object[]
                {
                    "RemovePartialCodeBlock",
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From(".ToString();"), SourceText.From(";"), GetPath("Simple.cshtml"), 1),
                        new TextReplacement(SourceText.From(" Foo"), SourceText.From(string.Empty), GetPath("View2.cshtml"), 1),
                        new TextReplacement(SourceText.From("Model[0];"), SourceText.From("Model;"), GetPath("View2.cshtml"), 7),
                    }
                }
            };

        private static string GetPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestViews", fileName);

        private static IEnumerable<TextReplacement> StageInputFiles(IEnumerable<TextReplacement> replacements)
        {
            var inputFiles = replacements.Select(r => r.FilePath).Distinct();

            foreach (var inputFile in inputFiles)
            {
                File.Copy(inputFile, Path.Combine(WorkingDir, Path.GetFileName(inputFile)), true);
            }

            return replacements.Select(r => new TextReplacement(r.OriginalText, r.NewText, Path.Combine(WorkingDir, Path.GetFileName(r.FilePath)), r.StartingLine));
        }
    }
}
