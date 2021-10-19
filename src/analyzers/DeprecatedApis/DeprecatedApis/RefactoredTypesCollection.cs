// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    internal class RefactoredTypesCollection
    {
        public static ImmutableHashSet<ISymbol> Create(Compilation compilation, ImmutableArray<AdditionalText> additionalTexts)
        {
            var result = ImmutableHashSet.CreateBuilder<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var line in GetNames(additionalTexts))
            {
                if (compilation.GetTypeByMetadataName(line) is ISymbol symbol)
                {
                    result.Add(symbol);
                }
            }

            return result.ToImmutable();
        }

        private static IEnumerable<string> GetNames(ImmutableArray<AdditionalText> additionalTexts)
        {
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

                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                yield return trimmed;
                            }
                        }
                    }
                }
            }

#if DEBUG
            yield return "System.Web.HttpContext";
            yield return "System.Web.HttpContextBase";
#endif
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
