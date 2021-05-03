﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class MappedSubTextTests
    {
        [Theory]
        [MemberData(nameof(EqualsTestData))]
        public void EqualsTests(MappedSubText a, MappedSubText b, bool expected)
        {
            Assert.Equal(expected, a.Equals(b));
            Assert.Equal(expected, b.Equals(a));
            Assert.Equal(expected, a.GetHashCode() == b.GetHashCode());
        }

        [Fact]
        public async Task GetMappedSubTextsNegativeTests()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => MappedSubText.GetMappedSubTextsAsync(null!, "Foo.cs", CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [MemberData(nameof(GetMappedSubTextsTestData))]
        public async Task GetMappedSubTextsTests(string source, string? defaultMapPath, IEnumerable<MappedSubText> expected)
        {
            // Arrange
            using var workspace = new AdhocWorkspace();
            var doc = CreateDoc(workspace, source);

            // Act
            var subTexts = await MappedSubText.GetMappedSubTextsAsync(doc, defaultMapPath, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Collection(subTexts, expected.Select<MappedSubText, Action<MappedSubText>>(e => a => Assert.Equal(e, a)).ToArray());
        }

        private static Document CreateDoc(AdhocWorkspace workspace, string source) =>
            workspace.AddProject("TestProject", "C#").AddDocument("TestDocument", SourceText.From(source));

        public static IEnumerable<object[]> EqualsTestData =>
            new List<object[]>
            {
                new object[]
                {
                    new MappedSubText(SourceText.From("a"), "Foo.txt", 83),
                    new MappedSubText(SourceText.From("a"), "foo.txt", 83),
                    true
                },
                new object[]
                {
                    new MappedSubText(SourceText.From("a"), "Foo.txt", 83),
                    new MappedSubText(SourceText.From("A"), "foo.txt", 83),
                    false
                },
                new object[]
                {
                    new MappedSubText(SourceText.From("a"), "foo.txt", 83),
                    new MappedSubText(SourceText.From("a"), "foo.tx", 83),
                    false
                },
                new object[]
                {
                    new MappedSubText(SourceText.From("a"), "foo.txt", 83),
                    new MappedSubText(SourceText.From("a"), "foo.txt", 84),
                    false
                },
                new object[]
                {
                    new MappedSubText(SourceText.From(string.Empty), "Foo.txt", 83),
                    new MappedSubText(SourceText.From("a"), "foo.txt", 83),
                    false
                },
                new object[]
                {
                    new MappedSubText(SourceText.From(string.Empty), "Foo.txt", 0),
                    new MappedSubText(SourceText.From(string.Empty), "foo.txt", 0),
                    true
                },
                new object[]
                {
                    new MappedSubText(SourceText.From("abc\ndef").GetSubText(4), string.Empty, 83),
                    new MappedSubText(SourceText.From("def"), string.Empty, 83),
                    true
                },
            };

        public static IEnumerable<object?[]> GetMappedSubTextsTestData =>
            new List<object?[]>
            {
                new object?[]
                {
                    string.Empty,
                    null,
                    Enumerable.Empty<MappedSubText>()
                },

                new object?[]
                {
                    string.Empty,
                    "C:\\Foo.cs",
                    new MappedSubText[]
                    {
                        new MappedSubText(SourceText.From(string.Empty), "C:\\Foo.cs", 0)
                    }
                },

                new object?[]
                {
                    File.ReadAllText("MappedSubTextTestData.cs"),
                    "Bar.cshtml",
                    new MappedSubText[]
                    {
                        new MappedSubText(SourceText.From("namespace Razor\r\n{"), "Bar.cshtml", 0),
                        new MappedSubText(SourceText.From("    using System;\r\n"), "test.cshtml", 1),
                        new MappedSubText(SourceText.From("\r\n            var foo = \"Hello World!\";\r\n"), "test.cshtml", 3),
                        new MappedSubText(SourceText.From("            __PTagHelper.FooProp = 123;\r\n"), "test.cshtml", 7),
                        new MappedSubText(SourceText.From("            WriteLiteral(foo);\r\n"), "test.cshtml", 7),
                    }
                }
            };
    }
}
