using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HttpContextIsDebuggingEnabledAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AM0006";
        private const string Category = "Migration";

        private const string MemberName = "IsDebuggingEnabled";
        private static readonly string[] HttpContextTypes = new[] { "System.Web.HttpContext", "Microsoft.AspNetCore.Http.HttpContext" };
        private static readonly string[] WellKnownInstances = new[] { "HttpContext.Current", "HttpContextHelper.Current" };

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HttpContextDebuggingEnabledTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HttpContextDebuggingEnabledMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HttpContextDebuggingEnabledDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberAccessExpressionSyntax expression)
            {
                return;
            }

            // If the member being accessed isn't named "IsDebuggingEnabled", bail out
            if (!expression.Name.Identifier.Text.Equals(MemberName, StringComparison.Ordinal))
            {
                return;
            }

            // Find the type containing the 'IsDebuggingEnabled' member
            var containingSymbolType = context.SemanticModel.GetSymbolInfo(expression.Expression).Symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propSymbol => propSymbol.Type,
                IMethodSymbol methodSymbol => methodSymbol.ReturnType,
                _ => null
            };

            if (containingSymbolType is null)
            {
                // If we can't resolve the expression's symbol, just check for syntax referring to a couple well-known
                // HttpContext properties.
                if (WellKnownInstances.Any(i => expression.Expression.ToString().EndsWith(i, StringComparison.Ordinal)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
                    return;
                }
            }
            else
            {
                // If we can resolve the expression's type, check to see if it is an HttpContext
                if (HttpContextTypes.Contains(containingSymbolType.ToString(), StringComparer.Ordinal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
                    return;
                }
            }
        }
    }
}
