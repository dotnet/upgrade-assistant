// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    public abstract class ApiControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0013";
        private const string Category = "Upgrade";

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
            return baseType.ToDisplayString().Equals($"{ApiControllerClassName}.{ApiControllerClassName}", StringComparison.Ordinal);
        }

        protected abstract void ReportDiagnostic(SymbolAnalysisContext context, SyntaxNode node);
    }
}
