// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AdapterDescriptorTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string AttributeDiagnosticId = "UA0014d";
        public const string ConstructorCountDiagnosticId = "UA0014e";
        public const string ParameterDiagnosticId = "UA0014f";
        public const string ParameterCountDiagnosticId = "UA0014g";

        private const string Category = "Refactor";

        private static readonly LocalizableString AdapterDescriptorTypeAttributeTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeAttributeMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeAttributeDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeAttributeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeConstructorCountDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeConstructorCountDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeParameterTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AdapterDescriptorTypeParameterCountTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterCountMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorTypeParameterCountDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeParameterCountDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor AttributeRule = new(AttributeDiagnosticId, AdapterDescriptorTypeAttributeTitle, AdapterDescriptorTypeAttributeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeAttributeDescription);
        private static readonly DiagnosticDescriptor ConstructorCountRule = new(ConstructorCountDiagnosticId, AdapterDescriptorTypeConstructorCountTitle, AdapterDescriptorTypeConstructorCountMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeConstructorCountDescription);
        private static readonly DiagnosticDescriptor ParameterRule = new(ParameterDiagnosticId, AdapterDescriptorTypeParameterTitle, AdapterDescriptorTypeParameterMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeParameterDescription);
        private static readonly DiagnosticDescriptor ParameterCountRule = new(ParameterCountDiagnosticId, AdapterDescriptorTypeParameterCountTitle, AdapterDescriptorTypeParameterCountMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorTypeParameterCountDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(AttributeRule, ConstructorCountRule, ParameterRule, ParameterCountRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSymbolAction(context =>
            {
                var types = WellKnownTypes.From(context.Symbol);

                if (types is null)
                {
                    return;
                }

                var systemType = context.Compilation.GetTypeByMetadataName("System.Type");

                if (systemType is null)
                {
                    return;
                }

                ValidateDescriptor(context, systemType, types.AdapterDescriptor);
            }, SymbolKind.NamedType);
        }

        private static void ValidateAttribute(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            var attribute = context.Compilation.GetTypeByMetadataName("System.Attribute");

            if (!SymbolEqualityComparer.Default.Equals(type.BaseType, attribute))
            {
                foreach (var location in type.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AttributeRule, location, type.ToDisplayString()));
                }
            }
        }

        private static void ValidateDescriptor(SymbolAnalysisContext context, INamedTypeSymbol systemType, INamedTypeSymbol? adapterDescriptor)
        {
            if (adapterDescriptor is null)
            {
                return;
            }

            ValidateAttribute(context, adapterDescriptor);

            if (adapterDescriptor.InstanceConstructors.Length > 1)
            {
                foreach (var location in adapterDescriptor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ConstructorCountRule, location, adapterDescriptor.ToDisplayString(), 1));
                }
            }

            var constructor = adapterDescriptor.InstanceConstructors[0];

            if (constructor.Parameters.Length != 2)
            {
                foreach (var location in constructor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ParameterCountRule, location, adapterDescriptor.ToDisplayString(), 2));
                }
            }

            foreach (var p in constructor.Parameters)
            {
                if (!SymbolEqualityComparer.Default.Equals(systemType, p.Type))
                {
                    foreach (var location in p.Locations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ParameterRule, location, adapterDescriptor.ToDisplayString(), systemType.ToDisplayString()));
                    }
                }
            }
        }
    }
}
