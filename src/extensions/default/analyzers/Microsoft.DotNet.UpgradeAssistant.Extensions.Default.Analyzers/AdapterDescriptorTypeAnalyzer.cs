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
        public const string AdapterDescriptorDiagnosticId = "UA0014d";

        private const string Category = "Refactor";

        private static readonly LocalizableString AdapterDescriptorTitle = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AdapterDescriptorDescription = new LocalizableResourceString(nameof(Resources.AdapterDescriptorTypeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor AdapterDescriptorRule = new(AdapterDescriptorDiagnosticId, AdapterDescriptorTitle, AdapterDescriptorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AdapterDescriptorDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(AdapterDescriptorRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationAction(context =>
            {
                var types = WellKnownTypes.From(context.Compilation);

                var systemType = context.Compilation.GetTypeByMetadataName("System.Type");

                if (systemType is null)
                {
                    return;
                }

                ValidateDescriptor(context, systemType, types.AdapterDescriptor);
            });
        }

        private static void ValidateAttribute(CompilationAnalysisContext context, INamedTypeSymbol adapterDescriptor)
        {
            var attribute = context.Compilation.GetTypeByMetadataName("System.Attribute");

            if (!SymbolEqualityComparer.Default.Equals(adapterDescriptor.BaseType, attribute))
            {
                foreach (var location in adapterDescriptor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AdapterDescriptorRule, location, Resources.AdapterDescriptorTypeMustBeAttribute));
                }
            }
        }

        private static void ValidateDescriptor(CompilationAnalysisContext context, INamedTypeSymbol systemType, INamedTypeSymbol? adapterDescriptor)
        {
            if (adapterDescriptor is null)
            {
                return;
            }

            ValidateAttribute(context, adapterDescriptor);

            if (adapterDescriptor.InstanceConstructors.Length != 1)
            {
                foreach (var location in adapterDescriptor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AdapterDescriptorRule, location, Resources.AdapterDescriptorTypeOnlyOneConstructor));
                }

                return;
            }

            var constructor = adapterDescriptor.InstanceConstructors[0];

            if (constructor.Parameters.Length != 2)
            {
                foreach (var location in constructor.Locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AdapterDescriptorRule, location, Resources.AdapterDescriptorTypeTwoParameters));
                }
            }

            foreach (var p in constructor.Parameters)
            {
                if (!SymbolEqualityComparer.Default.Equals(systemType, p.Type))
                {
                    foreach (var location in p.Locations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(AdapterDescriptorRule, location, Resources.AdapterDescriptorTypeMustBeAType));
                    }
                }
            }
        }
    }
}
