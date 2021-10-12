// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class AdapterDescriptorExtensions
    {
        private static SymbolDisplayFormat RoundtripMethodFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        private const string ExpectedTypeKey = nameof(ExpectedTypeKey);
        private const string MissingMethodTypeKey = nameof(MissingMethodTypeKey);
        private const string MissingMethodKey = nameof(MissingMethodKey);

        public static ImmutableDictionary<string, string?> WithMissingType(this ImmutableDictionary<string, string?> properties, ITypeSymbol symbol)
            => properties.Add(ExpectedTypeKey, symbol.ToDisplayString(RoundtripMethodFormat));

        public static bool TryGetExpectedTypeString(this ImmutableDictionary<string, string?> dictionary, [MaybeNullWhen(false)] out string result)
            => dictionary.TryGetValue(ExpectedTypeKey, out result) && result is not null;

        public static bool TryGetExpectedType(this ImmutableDictionary<string, string?> dictionary, SemanticModel semantic, [MaybeNullWhen(false)] out INamedTypeSymbol namedType)
        {
            if (dictionary.TryGetValue(ExpectedTypeKey, out var result) && result is not null)
            {
                if (semantic.Compilation.GetTypeByMetadataName(result) is INamedTypeSymbol symbol)
                {
                    namedType = symbol;
                    return true;
                }
            }

            namedType = default;
            return false;
        }

        public static ImmutableDictionary<string, string?> WithMissingMethod(this ImmutableDictionary<string, string?> properties, IMethodSymbol symbol)
            => properties
                .Add(MissingMethodKey, symbol.ToDisplayString(RoundtripMethodFormat))
                .Add(MissingMethodTypeKey, symbol.ContainingType.ToDisplayString(RoundtripMethodFormat));

        public static bool TryGetMissingMethod(this ImmutableDictionary<string, string?> dictionary, SemanticModel model, [MaybeNullWhen(false)] out IMethodSymbol method)
        {
            if (dictionary.TryGetValue(MissingMethodKey, out var missingMethodName) && missingMethodName is not null &&
                dictionary.TryGetValue(MissingMethodTypeKey, out var missingMethodType) && missingMethodType is not null)
            {
                if (model.Compilation.GetTypeByMetadataName(missingMethodType) is INamedTypeSymbol type)
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol && string.Equals(missingMethodName, member.ToDisplayString(RoundtripMethodFormat)))
                        {
                            method = methodSymbol;
                            return true;
                        }
                    }
                }
            }

            method = default;
            return false;
        }
    }
}
