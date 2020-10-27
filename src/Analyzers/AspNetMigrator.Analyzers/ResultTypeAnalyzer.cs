using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ResultTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AM0003";
        private const string Category = "Migration";
        private const string OldNamespace = "System.Web.Mvc";

        public static readonly Dictionary<string, string> MovedResultTypes = new Dictionary<string, string> 
        {
            { "ActionResult", "Microsoft.AspNetCore.Mvc.ActionResult" },
            { "ContentResult", "Microsoft.AspNetCore.Mvc.ContentResult" },
            { "FileResult", "Microsoft.AspNetCore.Mvc.FileResult" },
            { "HttpNotFoundResult", "Microsoft.AspNetCore.Mvc.NotFoundResult" },
            { "HttpStatusCodeResult", "Microsoft.AspNetCore.Mvc.StatusCodeResult" },
            { "HttpUnauthorizedResult", "Microsoft.AspNetCore.Mvc.UnauthorizedResult" },
            { "RedirectResult", "Microsoft.AspNetCore.Mvc.RedirectResult" },
            { "PartialViewResult", "Microsoft.AspNetCore.Mvc.PartialViewResult" },
            { "ViewResult", "Microsoft.AspNetCore.Mvc.ViewResult" }
        };


        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ResultTypeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ResultTypeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ResultTypeDescription), Resources.ResourceManager, typeof(Resources));

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
            if (name is null || !MovedResultTypes.Keys.Contains(name, StringComparer.Ordinal))
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't in the old namespace, bail out
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol;
            if (symbol != null && !symbol.ToString().Contains(OldNamespace))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), name, MovedResultTypes[name]);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
