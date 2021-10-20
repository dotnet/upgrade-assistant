// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public record AdapterContext
    {
        public ImmutableArray<AdapterDescriptor<ITypeSymbol>> TypeDescriptors { get; init; } = ImmutableArray.Create<AdapterDescriptor<ITypeSymbol>>();

        public ImmutableArray<AdapterDescriptor<ISymbol>> StaticMemberDescriptors { get; init; } = ImmutableArray.Create<AdapterDescriptor<ISymbol>>();

        public ImmutableArray<FactoryDescriptor> Factories { get; init; } = ImmutableArray.Create<FactoryDescriptor>();

        public ImmutableArray<AdapterDefinition> Definitions { get; init; } = ImmutableArray.Create<AdapterDefinition>();

        public WellKnownTypes Types { get; init; }

        public bool IsFactoryMethod(IMethodSymbol method)
        {
            foreach (var factory in Factories)
            {
                foreach (var factoryMethod in factory.Methods)
                {
                    if (SymbolEqualityComparer.Default.Equals(method, factoryMethod))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public AdapterDescriptor<ISymbol>? GetStaticMemberDescriptor(ISymbol symbol)
        {
            foreach (var descriptor in StaticMemberDescriptors)
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
                foreach (var method in factory.Methods)
                {
                    if (SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        public bool IsTypeMarkedForRefactoring(ISymbol type)
        {
            foreach (var definition in Definitions)
            {
                if (SymbolEqualityComparer.Default.Equals(definition.TypeToReplace, type))
                {
                    return true;
                }
            }

            foreach (var descriptor in TypeDescriptors)
            {
                if (SymbolEqualityComparer.Default.Equals(descriptor.Original, type))
                {
                    return true;
                }
            }

            return false;
        }

        public AdapterDescriptor<ITypeSymbol>? GetDescriptorForDestination(ITypeSymbol type)
        {
            foreach (var descriptor in TypeDescriptors)
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
                foreach (var factory in Factories)
                {
                    foreach (var method in factory.Methods)
                    {
                        if (SymbolEqualityComparer.Default.Equals(method, methodSymbol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static AdapterContext Create() => new();
    }
}
