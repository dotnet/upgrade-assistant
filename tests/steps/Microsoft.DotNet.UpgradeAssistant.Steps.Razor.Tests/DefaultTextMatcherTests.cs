// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using DiffPlex;
using DiffPlex.Chunkers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class DefaultTextMatcherTests
    {
        [Fact]
        public void MatchOrderedSubTextsNegativeTests()
        {
            var matcher = new DefaultTextMatcher(new Differ(), new CharacterChunker());
            var originalTexts = new MappedSubText[]
            {
                GetSubText("Test", "test.txt", 1)
            };
            var updatedTexts = new string[] { "Test1", "Test2" };

            Assert.Throws<ArgumentNullException>("originalTexts", () => matcher.MatchOrderedSubTexts(null!, updatedTexts));
            Assert.Throws<ArgumentNullException>("newTexts", () => matcher.MatchOrderedSubTexts(originalTexts, null!));
            Assert.Throws<ArgumentException>(() => matcher.MatchOrderedSubTexts(originalTexts, updatedTexts));
        }

        [Theory]
        [MemberData(nameof(MatchOrderedSubTextsData))]
        public void MatchOrderedSubTextsPositiveTests(IEnumerable<MappedSubText> originalTexts, IEnumerable<string> newTexts, IEnumerable<TextReplacement> expectedReplacements)
        {
            // Arrange
            var matcher = new DefaultTextMatcher(new Differ(), new CharacterChunker());

            // Act
            var replacements = matcher.MatchOrderedSubTexts(originalTexts, newTexts);

            // Assert
            Assert.Collection(replacements, expectedReplacements.Select<TextReplacement, Action<TextReplacement>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
        }

        public static IEnumerable<object[]> MatchOrderedSubTextsData =>
            new List<object[]>
            {
                // No original or updated texts
                new object[]
                {
                    Array.Empty<MappedSubText>(),
                    Array.Empty<string>(),
                    Array.Empty<TextReplacement>()
                },

                // Equal number of original and updated texts
                new object[]
                {
                    new MappedSubText[] { GetSubText("A", "A.txt", 10), GetSubText("B", "B.txt", 2), GetSubText("B", "B.txt", 2) },
                    new string[] { "C", string.Empty, "D" },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("A"), SourceText.From("C"), "A.txt", 10),
                        new TextReplacement(SourceText.From("B"), SourceText.From(string.Empty), "B.txt", 2),
                        new TextReplacement(SourceText.From("B"), SourceText.From("D"), "B.txt", 2),
                    }
                },

                // Unchange texts aren't returned
                new object[]
                {
                    new MappedSubText[] { GetSubText("A", "A.txt", 10), GetSubText("B", "B.txt", 2), GetSubText("C", "C.txt", 0) },
                    new string[] { "D", "B", "C " },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("A"), SourceText.From("D"), "A.txt", 10),
                        new TextReplacement(SourceText.From("C"), SourceText.From("C "), "C.txt", 0),
                    }
                },

                // Some texts removed
                new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "C", "F" },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Cat"), SourceText.From("C"), "A.txt", 10),
                        new TextReplacement(SourceText.From("Dog"), SourceText.From(string.Empty), "B.txt", 2),
                        new TextReplacement(SourceText.From("Fish"), SourceText.From("F"), "C.txt", 1000000000),
                    }
                },

                // Some texts removed (but pair with different updated text)
                new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "C", "o" },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Cat"), SourceText.From("C"), "A.txt", 10),
                        new TextReplacement(SourceText.From("Dog"), SourceText.From("o"), "B.txt", 2),
                        new TextReplacement(SourceText.From("Fish"), SourceText.From(string.Empty), "C.txt", 1000000000),
                    }
                },

                // Some texts removed (but pair with different updated text)
                new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "CDg", "-" },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Cat"), SourceText.From(string.Empty), "A.txt", 10),
                        new TextReplacement(SourceText.From("Dog"), SourceText.From("CDg"), "B.txt", 2),
                        new TextReplacement(SourceText.From("Fish"), SourceText.From("-"), "C.txt", 1000000000),
                    }
                },

                // Original texts can't be reordered
                new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "Fish", "Dog" },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Cat"), SourceText.From("Fish"), "A.txt", 10),
                        new TextReplacement(SourceText.From("Fish"), SourceText.From(string.Empty), "C.txt", 1000000000),
                    }
                },

                // Multiple texts removed; also check that file path comparison is case-insensitive
                new object[]
                {
                    new MappedSubText[]
                    {
                        GetSubText("Cat", "A.txt", 10),
                        GetSubText("Dog", "B.txt", 2),
                        GetSubText("Fish", "C.txt", 1000000000),
                        GetSubText("Bird Bird", "D.txt", 11),
                        GetSubText("  ", "E.txt", 2),
                    },
                    new string[] { "Cog", "  " },
                    new TextReplacement[]
                    {
                        new TextReplacement(SourceText.From("Cat"), SourceText.From(string.Empty), "a.txt", 10),
                        new TextReplacement(SourceText.From("Dog"), SourceText.From("Cog"), "b.txt", 2),
                        new TextReplacement(SourceText.From("Fish"), SourceText.From(string.Empty), "c.txt", 1000000000),
                        new TextReplacement(SourceText.From("Bird Bird"), SourceText.From(string.Empty), "d.txt", 11),
                    }
                },
            };

        private static MappedSubText GetSubText(string text, string filePath, int startingLine) =>
            new MappedSubText(SourceText.From(text), filePath, startingLine);
    }
}
