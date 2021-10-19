// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class MissingAdapterDescriptor : DiagnosticAnalyzer
    {
        public const string AddAdapterDescriptorDiagnosticId = "UA0111";

        private const string Category = "Refactor";

        private static readonly LocalizableString AddAdapterDescriptorTitle = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorMessageFormat = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddAdapterDescriptorDescription = new LocalizableResourceString(nameof(Resources.AddAdapterDescriptorDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor AddAdapterDescriptorRule = new(AddAdapterDescriptorDiagnosticId, AddAdapterDescriptorTitle, AddAdapterDescriptorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AddAdapterDescriptorDescription, helpLinkUri: HelpLink.Create(AddAdapterDescriptorDiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(AddAdapterDescriptorRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var adapterContext = AdapterContext.Create().FromCompilation(context.Compilation);
                var deprecatedTypeSymbols = RefactoredTypesCollection.Create(context.Compilation, context.Options.AdditionalFiles);

                context.RegisterTypeAdapterActions(adapterContext, context =>
                {
                    if (deprecatedTypeSymbols.Contains(context.symbol) && !adapterContext.IsTypeMarkedForRefactoring(context.symbol))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(AddAdapterDescriptorRule, context.node.GetLocation(), context.node.ToFullString().Trim()));
                    }
                });
            });
        }
    }
}
