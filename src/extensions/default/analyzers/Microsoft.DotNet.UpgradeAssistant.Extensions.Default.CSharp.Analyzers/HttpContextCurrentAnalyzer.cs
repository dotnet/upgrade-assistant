// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class HttpContextCurrentAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0005";
        private const string Category = "Upgrade";

        private const string TargetTypeSimpleName = "HttpContext";
        private const string TargetTypeSymbolName = "System.Web.HttpContext";
        private const string TargetMember = "Current";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HttpContextCurrentTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HttpContextCurrentMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HttpContextCurrentDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpressions, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeMemberAccessExpressions(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;

            // If the accessed member isn't named "Current" bail out
            if (!TargetMember.Equals(memberAccessExpression.Name.ToString(), StringComparison.Ordinal))
            {
                return;
            }

            // If the call is to a method called Current then bail out since they're
            // not using the HttpContext.Current property
            if (memberAccessExpression.Parent is InvocationExpressionSyntax)
            {
                return;
            }

            // Get the identifier accessed
            var accessedIdentifier = memberAccessExpression.Expression switch
            {
                IdentifierNameSyntax i => i,
                MemberAccessExpressionSyntax m => m.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault(),
                _ => null
            };

            // Return if the accessed identifier wasn't from a simple member access expression or identifier, or if it doesn't match HttpContext
            if (accessedIdentifier is null || !TargetTypeSimpleName.Equals(accessedIdentifier.Identifier.ValueText, StringComparison.Ordinal))
            {
                return;
            }

            // If the accessed identifier resolves to a type symbol other than System.Web.HttpContext, then bail out
            // since it means the user is calling some other similarly named API.
            var accessedSymbol = context.SemanticModel.GetSymbolInfo(accessedIdentifier).Symbol;
            if (accessedSymbol is INamedTypeSymbol symbol)
            {
                if (!symbol.ToDisplayString(NullableFlowState.NotNull).Equals(TargetTypeSymbolName, StringComparison.Ordinal))
                {
                    return;
                }
            }
            else if (accessedSymbol != null)
            {
                // If the accessed identifier resolves to a symbol other than a type symbol, bail out
                // since it's not a reference to System.Web.HttpContext
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
