// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class TypeMapLoaderTests
    {
        private static readonly AdditionalText[] Texts = new AdditionalText[]
        {
                new TestAdditionalText("File.typemap", null),
                new TestAdditionalText("Other file.typemap", string.Empty),
                new TestAdditionalText(@"C:\Foo\bar\x.typemap", @"a b
                  Test1     Test2
                  x     

            
                  y
                  1                                     3       2
                  4      5


        "),
                new TestAdditionalText("NotTypeMap.txt", "ABC\tDEF"),
                new TestAdditionalText("File.typemap", null),
                new TestAdditionalText("/a/b/c.typemap", "Foo                Bar   "),
        };

        [Fact]
        public static void WebTypeReplacementContentTests()
        {
            // Arrange
            var input = Texts.ToImmutableArray();
            var expectedMaps = new TypeMapping[]
            {
                new TypeMapping("a", "b"),
                new TypeMapping("Test1", "Test2"),
                new TypeMapping("x", null),
                new TypeMapping("y", null),
                new TypeMapping("4", "5"),
                new TypeMapping("Foo", "Bar")
            };

            // Act
            var empty = TypeMapLoader.LoadMappings(ImmutableArray.CreateRange(Enumerable.Empty<AdditionalText>()));
            var typeMaps = TypeMapLoader.LoadMappings(input);

            // Assert
            Assert.Empty(empty);
            Assert.Collection(typeMaps, expectedMaps.Select<TypeMapping, Action<TypeMapping>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
        }

        private class TestAdditionalText : AdditionalText
        {
            private readonly string? _text;

            public override string Path { get; }

            public TestAdditionalText(string path, string? text)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                _text = text;
            }

            public override SourceText? GetText(CancellationToken cancellationToken = default) =>
                _text is null
                ? null
                : SourceText.From(_text);
        }
    }
}
