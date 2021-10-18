// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    internal class RefactoredTypesCollection
    {
        public static ImmutableHashSet<ISymbol> Create(Compilation compilation, ImmutableArray<AdditionalText> additionalTexts)
        {
            var result = ImmutableHashSet.CreateBuilder<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var additional in additionalTexts)
            {
                if (additional.Path.EndsWith(".refactoring.txt", StringComparison.OrdinalIgnoreCase))
                {
                    var text = additional.GetText();

                    if (text is not null)
                    {
                        foreach (var line in text.Lines)
                        {
                            var trimmed = TrimLine(line.ToString());

                            if (trimmed.Length > 0 && compilation.GetTypeByMetadataName(trimmed) is ISymbol symbol)
                            {
                                result.Add(symbol);
                            }
                        }
                    }
                }
            }

            return result.ToImmutable();
        }

        private static string TrimLine(string input)
        {
            var span = input.AsSpan();
            var idx = input.IndexOf('#');

            if (idx >= 0)
            {
                span = span.Slice(0, idx);
            }

            return span.Trim().ToString();
        }
    }
}
