// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ApiControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0013";
        private const string Category = "Upgrade";

        public const string ApiControllerQualifiedName = ApiControllerNamespace + "." + ApiControllerClassName;
        public const string ApiControllerNamespace = "System.Web.Http";
        public const string ApiControllerClassName = "ApiController";

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

            if (IsBaseTypeAQualifiedReferenceToApiController(baseType)
                || IsBaseTypeAnImplicitReferenceToApiController(baseType))
            {
                // For all such symbols, produce a diagnostic.
                var node = namedTypeSymbol.DeclaringSyntaxReferences[0].GetSyntax();
                ReportDiagnostic(context, node);
            }
        }

        private static bool IsBaseTypeAnImplicitReferenceToApiController(INamedTypeSymbol baseType)
        {
            return baseType.ToDisplayString().Equals(ApiControllerClassName, StringComparison.Ordinal) && baseType.TypeKind == TypeKind.Error;
        }

        private static bool IsBaseTypeAQualifiedReferenceToApiController(INamedTypeSymbol baseType)
        {
            // remembering to be cautious that string operations are immutable. We create a unique constant for this conditional
            // to prevent situations where evaluating this analyzer on large solutions could lead to excessive garbage collection.
            return baseType.ToDisplayString().Equals(ApiControllerQualifiedName, StringComparison.Ordinal);
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, SyntaxNode node)
        {
            // Note that we choose to report the diagnostic on the ClassStatementSyntax from the namedTypeSymbol because
            // this simplifies the code maintenance for the Analyzer with the potential cost of a lower UX when running the analyzer in VS
            //
            // This is in contrast to reporting the diagnostic on the BaseTypeSyntax and highlighting ApiController specifically
            // 1. namedTypeSymbol.baseType does not have a DeclaringSyntaxReferences[0] that is in the code. The reference is in the metadata.
            // 2. highlighting the baseType means syntax analysis which we would implement in a language specific way in a child class
            var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), node.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
