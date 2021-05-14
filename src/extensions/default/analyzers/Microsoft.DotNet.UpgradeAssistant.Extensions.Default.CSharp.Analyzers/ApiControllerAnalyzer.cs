// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ApiControllerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ApiControllerMessageFormat), Resources.ResourceManager, typeof(Resources));
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

            // noted that highlighting the syntax makes it easier to handle partial class scenarios because
            // the analyzer will be limited to scenarios where a base type has been defined via syntax
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, CodeAnalysis.CSharp.SyntaxKind.BaseList);
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, CodeAnalysis.VisualBasic.SyntaxKind.InheritsStatement);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node; // an ImportStatementSyntax for VB or a BaseListSyntax for CS

            if (!node.DescendantNodes().Any())
            {
                return;
            }

            // the first descendent of ImportStatementSyntax will be a QualifiedNameSyntax or IdentifierNameSyntax for VB
            // the first descendent of BaseListSyntax will be a SimpleBaseTypeSyntax for CS
            var baseTypeNode = context.Node.DescendantNodes().First();

            if (baseTypeNode.IsKind(CodeAnalysis.CSharp.SyntaxKind.SimpleBaseType) && baseTypeNode.DescendantNodes().Any())
            {
                // In CSharp syntax, the SimpleBaseTypeSyntax the first child and we want the QualifiedNameSyntax or IdentifierNameSyntax
                // resolving this node must be done before we check for symbol info
                baseTypeNode = baseTypeNode.DescendantNodes().First();
            }

            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseTypeNode).Symbol as INamedTypeSymbol;
            if (baseTypeSymbol is not null
                && !IsBaseTypeAQualifiedReferenceToApiController(baseTypeSymbol.ToDisplayString()))
            {
                // we found a symbol, and the symbol information tells us this isn't an ApiController
                // bail out

                // note: at this point of upgrade assistant's workflow we are expecting the System.Web.Http reference
                // to have been removed so if we didn't find the symbol, that's okay
                return;
            }

            if (IsBaseTypeAQualifiedReferenceToApiController(baseTypeNode.ToString())
                || IsBaseTypeAnImplicitReferenceToApiController(baseTypeNode.ToString()))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, baseTypeNode.GetLocation(), baseTypeNode.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Checks to see if an instance of ApiController is found.
        /// </summary>
        /// <param name="baseTypeString">the content of a syntax node.</param>
        /// <returns>true if the string is equal to ApiController.</returns>
        private static bool IsBaseTypeAnImplicitReferenceToApiController(string baseTypeString)
        {
            return baseTypeString.Equals(ApiControllerClassName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks to see if an instance of System.Web.Http.ApiController is found.
        /// </summary>
        /// <param name="baseTypeString">the content of a syntax node.</param>
        /// <returns>true if the string is equal to System.Web.Http.ApiController.</returns>
        private static bool IsBaseTypeAQualifiedReferenceToApiController(string baseTypeString)
        {
            // remembering to be cautious that string operations are immutable. We create a unique constant for this conditional
            // to prevent situations where evaluating this analyzer on large solutions could lead to excessive garbage collection.
            return baseTypeString.Equals(ApiControllerQualifiedName, StringComparison.Ordinal);
        }
    }
}
