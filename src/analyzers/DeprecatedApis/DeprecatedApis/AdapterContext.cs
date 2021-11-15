// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public record AdapterContext
    {
        public ImmutableArray<ReplacementDescriptor<ITypeSymbol>> Types { get; init; } = ImmutableArray.Create<ReplacementDescriptor<ITypeSymbol>>();

        public ImmutableArray<ReplacementDescriptor<ISymbol>> StaticMembers { get; init; } = ImmutableArray.Create<ReplacementDescriptor<ISymbol>>();

        public ImmutableArray<FactoryDescriptor> Factories { get; init; } = ImmutableArray.Create<FactoryDescriptor>();

        public ImmutableArray<ApiDescriptor> Apis { get; init; } = ImmutableArray.Create<ApiDescriptor>();

        public WellKnownTypes KnownTypes { get; init; }

        public bool IsFactoryMethod(IMethodSymbol method)
        {
            foreach (var factory in Factories)
            {
                if (SymbolEqualityComparer.Default.Equals(method, factory.Method))
                {
                    return true;
                }
            }

            return false;
        }

        public ReplacementDescriptor<ISymbol>? GetStaticMemberDescriptor(ISymbol symbol)
        {
            foreach (var descriptor in StaticMembers)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Original, symbol))
                {
                    return descriptor;
                }
            }

            return null;
        }

        public IMethodSymbol? GetFactory(ITypeSymbol type)
        {
            foreach (var factory in Factories)
            {
                if (SymbolEqualityComparer.Default.Equals(factory.Method.ReturnType, type))
                {
                    return factory.Method;
                }
            }

            return null;
        }

        public bool IsTypeMarkedForRefactoring(ISymbol type)
        {
            foreach (var definition in Apis)
            {
                if (SymbolEqualityComparer.Default.Equals(definition.TypeToReplace, type))
                {
                    return true;
                }
            }

            foreach (var descriptor in Types)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Original, type))
                {
                    return true;
                }
            }

            return false;
        }

        public ReplacementDescriptor<ITypeSymbol>? GetDescriptorForDestination(ITypeSymbol type)
        {
            foreach (var descriptor in Types)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Destination, type))
                {
                    return descriptor;
                }
            }

            return null;
        }

        public bool Ignore(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return false;
            }

            if (symbol is IMethodSymbol methodSymbol)
            {
                return IsFactoryMethod(methodSymbol);
            }

            return false;
        }

        public static AdapterContext Create() => new();
    }
}
