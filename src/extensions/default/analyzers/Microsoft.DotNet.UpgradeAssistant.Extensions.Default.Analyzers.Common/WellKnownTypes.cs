// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record WellKnownTypes
    {
        public INamedTypeSymbol? AdapterDescriptor { get; init; }

        public INamedTypeSymbol? DescriptorFactory { get; init; }

        public INamedTypeSymbol? AdapterIgnore { get; init; }

        public static WellKnownTypes From(Compilation compilation)
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return new()
            {
                AdapterDescriptor = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Refactoring.AdapterDescriptorAttribute"),
                DescriptorFactory = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Refactoring.AdapterFactoryDescriptorAttribute"),
                AdapterIgnore = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Refactoring.AdapterIgnoreAttribute"),
            };
        }
    }
}
