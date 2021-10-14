// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record WellKnownTypes
    {
        private const string AdapterDescriptorFullyQualified = "Microsoft.CodeAnalysis.Refactoring.AdapterDescriptorAttribute";
        private const string FactoryDescriptorFullyQualified = "Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptorAttribute";

        private static readonly SymbolDisplayFormat DisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        public INamedTypeSymbol? AdapterDescriptor { get; init; }

        public INamedTypeSymbol? DescriptorFactory { get; init; }

        public static WellKnownTypes? From(ISymbol? symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            if (symbol is IMethodSymbol method)
            {
                symbol = method.ContainingType;
            }

            if (symbol is not INamedTypeSymbol namedType)
            {
                return null;
            }

            return namedType.ToDisplayString(DisplayFormat) switch
            {
                AdapterDescriptorFullyQualified => new() { AdapterDescriptor = namedType },
                FactoryDescriptorFullyQualified => new() { DescriptorFactory = namedType },
                _ => null,
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
                AdapterDescriptor = compilation.GetTypeByMetadataName(AdapterDescriptorFullyQualified),
                DescriptorFactory = compilation.GetTypeByMetadataName(FactoryDescriptorFullyQualified),
            };
        }
    }
}
