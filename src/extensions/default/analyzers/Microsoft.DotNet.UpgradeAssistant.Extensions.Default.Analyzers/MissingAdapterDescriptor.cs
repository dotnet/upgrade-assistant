// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class MissingAdapterDescriptor : DiagnosticAnalyzer
    {
        public const string AddAdapterDescriptorDiagnosticId = "UA0014l";

        private const string Category = "Refactor";

        private static readonly LocalizableString AddAdapterDescriptorTitle = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorMessageFormat = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorDescription = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor AddAdapterDescriptorRule = new(AddAdapterDescriptorDiagnosticId, AddAdapterDescriptorTitle, AddAdapterDescriptorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AddAdapterDescriptorDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(AddAdapterDescriptorRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var adapterContext = AdapterContext.Create().FromCompilation(context.Compilation);
                var deprecatedTypeSymbols = InitializeDeprecatedTypeSymbols(context.Compilation);

                if (adapterContext.Types.AdapterDescriptor is null)
                {
                    context.RegisterTypeAdapterActions(adapterContext, context =>
                    {
                        if (deprecatedTypeSymbols.Contains(context.symbol))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(AddAdapterDescriptorRule, context.node.GetLocation(), context.node.ToFullString().Trim()));
                        }
                    });
                }
            });
        }

        private static HashSet<ISymbol> InitializeDeprecatedTypeSymbols(Compilation compilation)
        {
            var deprecatedTypeNames = new[]
            {
                "System.Web.HttpContext",
                "System.Web.HttpContextBase",
                "Microsoft.AspNetCore.Http.HttpRequest",
                "Microsoft.AspNetCore.Http.HttpResponse",
            };

#pragma warning disable RS1024 // Compare symbols correctly (Known false positive: https://github.com/dotnet/roslyn-analyzers/issues/4568)
            var deprecatedTypeSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

            foreach (var name in deprecatedTypeNames)
            {
                var typeSymbol = compilation.GetTypeByMetadataName(name);
                if (typeSymbol is not null)
                {
                    deprecatedTypeSymbols.Add(typeSymbol);
                }
            }

            return deprecatedTypeSymbols;
        }
    }
}
