// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;

using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore | ProjectComponents.AspNet)]
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class HttpContextCurrentAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0005";
        private const string Category = "Upgrade";

        private const string TargetTypeSimpleName = "HttpContext";
        private const string TargetMember = "Current";
        private const string TargetPropertySymbolName = "Microsoft.AspNetCore.Mvc.ControllerBase.HttpContext";

        private static readonly string[] TargetTypeSymbolNames = new[] { "System.Web.HttpContext", "Microsoft.AspNetCore.Http.HttpContext" };
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HttpContextCurrentTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HttpContextCurrentMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HttpContextCurrentDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: false, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSimpleMemberAccessExpression(AnalyzeMemberAccessExpressionsCsharp, AnalyzeMemberAccessExpressionsVb);
        }

        private void AnalyzeMemberAccessExpressionsCsharp(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (CSSyntax.MemberAccessExpressionSyntax)context.Node;

            // If the accessed member isn't named "Current" bail out
            if (!TargetMember.Equals(memberAccessExpression.Name.Identifier.ValueText, StringComparison.Ordinal))
            {
                return;
            }

            // If the call is to a method called Current then bail out since they're
            // not using the HttpContext.Current property
            if (memberAccessExpression.Parent is CSSyntax.InvocationExpressionSyntax)
            {
                return;
            }

            // Get the identifier accessed
            var accessedIdentifier = memberAccessExpression.Expression switch
            {
                CSSyntax.IdentifierNameSyntax i => i,
                CSSyntax.MemberAccessExpressionSyntax m => m.DescendantNodes().OfType<CSSyntax.IdentifierNameSyntax>().LastOrDefault(),
                _ => null
            };

            AnalyzeMemberAccessExpressions(context, memberAccessExpression, accessedIdentifier, accessedIdentifier?.Identifier.ValueText);
        }

        private void AnalyzeMemberAccessExpressionsVb(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (VBSyntax.MemberAccessExpressionSyntax)context.Node;

            // If the accessed member isn't named "Current" bail out
            if (!TargetMember.Equals(memberAccessExpression.Name.Identifier.ValueText, StringComparison.Ordinal))
            {
                return;
            }

            // If the call is to a method called Current then bail out since they're
            // not using the HttpContext.Current property
            if (memberAccessExpression.Parent is VBSyntax.InvocationExpressionSyntax)
            {
                return;
            }

            // Get the identifier accessed
            var accessedIdentifier = memberAccessExpression.Expression switch
            {
                VBSyntax.IdentifierNameSyntax i => i,
                VBSyntax.MemberAccessExpressionSyntax m => m.DescendantNodes().OfType<VBSyntax.IdentifierNameSyntax>().LastOrDefault(),
                _ => null
            };

            AnalyzeMemberAccessExpressions(context, memberAccessExpression, accessedIdentifier, accessedIdentifier?.Identifier.ValueText);
        }

        private static void AnalyzeMemberAccessExpressions(SyntaxNodeAnalysisContext context, SyntaxNode memberAccessExpression, SyntaxNode? accessedIdentifier, string? identifierValue)
        {
            // Return if the accessed identifier wasn't from a simple member access expression or identifier, or if it doesn't match HttpContext
            if (accessedIdentifier is null || !TargetTypeSimpleName.Equals(identifierValue, StringComparison.Ordinal))
            {
                return;
            }

            if (!TryMatchSymbol(context, accessedIdentifier))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Attempts to match against an identifier's symbol against ASP.NET's HttpContext.
        /// If a symbol is resolved, it must match System.Web.HttpContext or
        /// Microsoft.AspNetCore.Http.HttpContext exactly.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        /// <param name="accessedIdentifier">The accessedIdentifier that was found.</param>
        /// <returns>Returns true if the identifier's symbol matches ASP.NET's HttpContext or if no symbol was found.</returns>
        private static bool TryMatchSymbol(SyntaxNodeAnalysisContext context, SyntaxNode accessedIdentifier)
        {
            // If the accessed identifier resolves to a type symbol other than System.Web.HttpContext or
            // Microsoft.AspNetCore.Http.HttpContext, then bail out since it means the user is calling
            // some other similarly named API. This allows diagnostics on Microsoft.AspNetCore.Http.HttpContext
            // in addition to System.Web.HttpContext since ASP.NET Core's HttpContext doesn't have a Current
            // property so any attempt to access such a property probably indicates a partially upgraded
            // call site that needs remedied.
            var accessedSymbol = context.SemanticModel.GetSymbolInfo(accessedIdentifier).Symbol;
            if (accessedSymbol is INamedTypeSymbol symbol)
            {
                if (!TargetTypeSymbolNames.Any(name => symbol.ToDisplayString(NullableFlowState.NotNull).Equals(name, StringComparison.Ordinal)))
                {
                    return false;
                }
            }
            else if (accessedSymbol is IPropertySymbol propSymbol)
            {
                // If the HttpContext reference occurs inside a controller, HttpContext.Current can look
                // like a reference to the ControllerBase.HttpContext property. However, these should still
                // be flagged because .Current won't exist in ASP.NET Core. Therefore, bail out for
                // property symbols only if they are not ControllerBase.HttpContext.
                if (!TargetPropertySymbolName.Equals(propSymbol.ToDisplayString(), StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else if (accessedSymbol != null)
            {
                // If the accessed identifier resolves to a symbol other than a type or property symbol, bail out
                // since it's not a reference to System.Web.HttpContext
                return false;
            }

            return true;
        }
    }
}
