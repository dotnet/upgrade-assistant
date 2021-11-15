// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    public readonly record struct WellKnownTypes
    {
        private static readonly SymbolDisplayFormat DisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        public bool IsEmpty => AdapterDescriptor is null && DescriptorFactory is null && AdapterStaticDescriptor is null;

        public INamedTypeSymbol? AdapterDescriptor { get; init; }

        public INamedTypeSymbol? AdapterStaticDescriptor { get; init; }

        public INamedTypeSymbol? DescriptorFactory { get; init; }

        public static WellKnownTypes From(IOperation operation)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return From(operation.SemanticModel.GetSymbolInfo(operation.Syntax).Symbol);
        }

        public static WellKnownTypes From(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return default;
            }

            if (symbol is IMethodSymbol method)
            {
                symbol = method.ContainingType;
            }

            if (symbol is not INamedTypeSymbol namedType)
            {
                return default;
            }

            return namedType.ToDisplayString(DisplayFormat) switch
            {
                WellKnownTypeNames.AdapterDescriptorFullyQualified => new() { AdapterDescriptor = namedType },
                WellKnownTypeNames.AdapterStaticDescriptorFullyQualified => new() { AdapterStaticDescriptor = namedType },
                WellKnownTypeNames.FactoryDescriptorFullyQualified => new() { DescriptorFactory = namedType },
                _ => default,
            };
        }

        public static WellKnownTypes From(Compilation compilation)
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return new()
            {
                AdapterDescriptor = compilation.GetTypeByMetadataName(WellKnownTypeNames.AdapterDescriptorFullyQualified),
                AdapterStaticDescriptor = compilation.GetTypeByMetadataName(WellKnownTypeNames.AdapterStaticDescriptorFullyQualified),
                DescriptorFactory = compilation.GetTypeByMetadataName(WellKnownTypeNames.FactoryDescriptorFullyQualified),
            };
        }
    }
}
