// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class AdapterDescriptorExtensions
    {
        private const string ExpectedTypeKey = nameof(ExpectedTypeKey);
        private const string MemberTypeKey = nameof(MemberTypeKey);
        private const string MemberKey = nameof(MemberKey);
        private const string MemberNameKey = nameof(MemberNameKey);

        private static readonly SymbolDisplayFormat RoundtripMethodFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        public static ImmutableDictionary<string, string?> WithSymbol(this ImmutableDictionary<string, string?> properties, ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return properties.Add(ExpectedTypeKey, namedType.ToDisplayString(RoundtripMethodFormat));
            }
            else if (symbol is IPropertySymbol || symbol is IMethodSymbol)
            {
                return properties
                    .Add(MemberNameKey, symbol.Name)
                    .Add(MemberKey, symbol.ToDisplayString(RoundtripMethodFormat))
                    .Add(MemberTypeKey, symbol.ContainingType.ToDisplayString(RoundtripMethodFormat));
            }

            return properties;
        }

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

        public static bool TryGetMissingMember(this ImmutableDictionary<string, string?> dictionary, SemanticModel model, [MaybeNullWhen(false)] out ISymbol method)
        {
            if (dictionary.TryGetValue(MemberKey, out var missingMethod) && missingMethod is not null &&
                dictionary.TryGetValue(MemberTypeKey, out var missingMethodType) && missingMethodType is not null &&
                dictionary.TryGetValue(MemberNameKey, out var missingMethodName) && missingMethodName is not null)
            {
                if (model.Compilation.GetTypeByMetadataName(missingMethodType) is INamedTypeSymbol type)
                {
                    foreach (var member in type.GetMembers(missingMethodName))
                    {
                        if (string.Equals(missingMethod, member.ToDisplayString(RoundtripMethodFormat), System.StringComparison.Ordinal))
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
