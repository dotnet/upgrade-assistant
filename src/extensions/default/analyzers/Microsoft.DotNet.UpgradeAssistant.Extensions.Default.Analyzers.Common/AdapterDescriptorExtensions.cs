// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class AdapterDescriptorExtensions
    {
        private const string ExpectedTypeKey = nameof(ExpectedTypeKey);
        private const string MissingMethodTypeKey = nameof(MissingMethodTypeKey);
        private const string MissingMethodKey = nameof(MissingMethodKey);
        private const string MissingMethodNameKey = nameof(MissingMethodNameKey);

        private static readonly SymbolDisplayFormat RoundtripMethodFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        public static ImmutableDictionary<string, string?> WithMissingType(this ImmutableDictionary<string, string?> properties, ITypeSymbol symbol)
            => properties.Add(ExpectedTypeKey, symbol.ToDisplayString(RoundtripMethodFormat));

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

        public static ImmutableDictionary<string, string?> WithMissingMember(this ImmutableDictionary<string, string?> properties, ISymbol symbol)
            => properties
                .Add(MissingMethodNameKey, symbol.Name)
                .Add(MissingMethodKey, symbol.ToDisplayString(RoundtripMethodFormat))
                .Add(MissingMethodTypeKey, symbol.ContainingType.ToDisplayString(RoundtripMethodFormat));

        public static bool TryGetMissingMember(this ImmutableDictionary<string, string?> dictionary, SemanticModel model, [MaybeNullWhen(false)] out ISymbol method)
        {
            if (dictionary.TryGetValue(MissingMethodKey, out var missingMethod) && missingMethod is not null &&
                dictionary.TryGetValue(MissingMethodTypeKey, out var missingMethodType) && missingMethodType is not null &&
                dictionary.TryGetValue(MissingMethodNameKey, out var missingMethodName) && missingMethodName is not null)
            {
                if (model.Compilation.GetTypeByMetadataName(missingMethodType) is INamedTypeSymbol type)
                {
                    foreach (var member in type.GetMembers(missingMethodName))
                    {
                        if (string.Equals(missingMethod, member.ToDisplayString(RoundtripMethodFormat)))
                        {
                            method = member;
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
