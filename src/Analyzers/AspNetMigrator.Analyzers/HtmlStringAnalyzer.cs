using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HtmlStringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AM0002";
        private const string Category = "Migration";

        // Potentially problematic identifiers to look for
        private static readonly string[] ObsoleteHtmlStringTypes = new[] { "IHtmlString", "MvcHtmlString", "HtmlString" };

        // Namespace-qualified types that shouldn't be referenced
        private static readonly string[] ObsoleteHtmlStringFullTypes = new[] { "System.Web.IHtmlString", "System.Web.Mvc.MvcHtmlString", "System.Web.HtmlString" };

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HtmlStringTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HtmlStringMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HtmlStringDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeIdentifiers, SyntaxKind.IdentifierName);
        }

        private void AnalyzeIdentifiers(SyntaxNodeAnalysisContext context)
        {
            var identifier = context.Node as IdentifierNameSyntax;
            var name = identifier?.Identifier.ValueText;

            // If the node isn't an identifier or isn't one of the indicated HtmlString types, bail out
            if (name is null || !ObsoleteHtmlStringTypes.Contains(name, StringComparer.Ordinal))
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't prohibited, bail out
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol;
            if (symbol != null && !ObsoleteHtmlStringFullTypes.Contains(symbol.ToString(), StringComparer.Ordinal))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
