// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public abstract class ApiControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0013";
        private const string Category = "Upgrade";

        public const string BadNamespace = "System.Web.Http";
        public const string BadClassName = "ApiController";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));

        protected static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSymbolAction(AnalyzeSymbols, SymbolKind.NamedType);
        }

        private void AnalyzeSymbols(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            var baseType = namedTypeSymbol.BaseType;

            if (baseType is null)
            {
                return;
            }

            // Find just the named type symbols with names containing lowercase letters.
            if (baseType.ToDisplayString().Equals("System.Web.Http.ApiController", StringComparison.Ordinal))
            {
                // For all such symbols, produce a diagnostic.
                var node = namedTypeSymbol.DeclaringSyntaxReferences[0].GetSyntax();

                ReportDiagnostic(context, node);
            }
        }

        protected abstract void ReportDiagnostic(SymbolAnalysisContext context, SyntaxNode node);

        public static Project AddMetadataReferences(Project project)
        {
            if (project is null)
            {
                return project!;
            }

            // todo - still an open question about how we locate metadatareferences
            const string assemblyFolder = @"C:\deleteMe\";
            var assemblyPath = Path.Combine(assemblyFolder, $"System.Web.Http.dll");

            return project.AddMetadataReference(MetadataReference.CreateFromFile(assemblyPath));
        }
    }
}
