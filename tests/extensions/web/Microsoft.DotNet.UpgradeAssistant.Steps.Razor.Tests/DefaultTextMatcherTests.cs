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
        public void CtorNegativeTests()
        {
            Assert.Throws<ArgumentNullException>("differ", () => new DefaultTextMatcher(null!, new CharacterChunker()));
            Assert.Throws<ArgumentNullException>("chunker", () => new DefaultTextMatcher(new Differ(), null!));
        }

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
        public void MatchOrderedSubTextsPositiveTests(IEnumerable<MappedSubText> originalTexts, IEnumerable<string> newTexts, IEnumerable<MappedTextReplacement> expectedReplacements)
        {
            // Arrange
            var matcher = new DefaultTextMatcher(new Differ(), new CharacterChunker());

            // Act
            var replacements = matcher.MatchOrderedSubTexts(originalTexts, newTexts);

            // Assert
            Assert.Collection(replacements, expectedReplacements.Select<MappedTextReplacement, Action<MappedTextReplacement>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
        }

        public static IEnumerable<object[]> MatchOrderedSubTextsData
        {
            get
            {
                // No original or updated texts
                yield return new object[]
                {
                    Array.Empty<MappedSubText>(),
                    Array.Empty<string>(),
                    Array.Empty<MappedTextReplacement>()
                };

                // Equal number of original and updated texts
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("A", "A.txt", 10), GetSubText("B", "B.txt", 2), GetSubText("B", "B.txt", 2) },
                    new string[] { "C", string.Empty, "D" },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("A", "C", "A.txt", 10),
                        new MappedTextReplacement("B", string.Empty, "B.txt", 2),
                        new MappedTextReplacement("B", "D", "B.txt", 2),
                    }
                };

                // All texts removed
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("A", "A.txt", 10), GetSubText("B", "B.txt", 2), GetSubText("C", "B.txt", 2) },
                    Array.Empty<string>(),
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("A", string.Empty, "A.txt", 10),
                        new MappedTextReplacement("B", string.Empty, "B.txt", 2),
                        new MappedTextReplacement("C", string.Empty, "B.txt", 2),
                    }
                };

                // Unchange texts aren't returned
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("A", "A.txt", 10), GetSubText("B", "B.txt", 2), GetSubText("C", "C.txt", 0) },
                    new string[] { "D", "B", "C " },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("A", "D", "A.txt", 10),
                        new MappedTextReplacement("C", "C ", "C.txt", 0),
                    }
                };

                // Some texts removed
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "C", "F" },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Cat", "C", "A.txt", 10),
                        new MappedTextReplacement("Dog", string.Empty, "B.txt", 2),
                        new MappedTextReplacement("Fish", "F", "C.txt", 1000000000),
                    }
                };

                // Some texts removed (but pair with different updated text)
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "C", "o" },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Cat", "C", "A.txt", 10),
                        new MappedTextReplacement("Dog", "o", "B.txt", 2),
                        new MappedTextReplacement("Fish", string.Empty, "C.txt", 1000000000),
                    }
                };

                // Some texts removed (but pair with different updated text)
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "CDg", "-" },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Cat", string.Empty, "A.txt", 10),
                        new MappedTextReplacement("Dog", "CDg", "B.txt", 2),
                        new MappedTextReplacement("Fish", "-", "C.txt", 1000000000),
                    }
                };

                // Original texts can't be reordered
                yield return new object[]
                {
                    new MappedSubText[] { GetSubText("Cat", "A.txt", 10), GetSubText("Dog", "B.txt", 2), GetSubText("Fish", "C.txt", 1000000000) },
                    new string[] { "Fish", "Dog" },
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Cat", "Fish", "A.txt", 10),
                        new MappedTextReplacement("Fish", string.Empty, "C.txt", 1000000000),
                    }
                };

                // Multiple texts removed; also check that file path comparison is case-insensitive
                yield return new object[]
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
                    new MappedTextReplacement[]
                    {
                        new MappedTextReplacement("Cat", string.Empty, "a.txt", 10),
                        new MappedTextReplacement("Dog", "Cog", "b.txt", 2),
                        new MappedTextReplacement("Fish", string.Empty, "c.txt", 1000000000),
                        new MappedTextReplacement("Bird Bird", string.Empty, "d.txt", 11),
                    }
                };
            }
        }

        private static MappedSubText GetSubText(string text, string filePath, int startingLine) =>
            new MappedSubText(SourceText.From(text), filePath, startingLine);
    }
}
