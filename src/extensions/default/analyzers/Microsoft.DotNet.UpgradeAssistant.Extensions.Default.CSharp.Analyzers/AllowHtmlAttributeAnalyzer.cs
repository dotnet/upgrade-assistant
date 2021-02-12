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
    public class AllowHtmlAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AM0010";
        private const string Category = "Migration";
        private const string AllowHtmlAttributeName = "System.Web.Mvc.AllowHtmlAttribute";

        private static readonly string[] DisallowedNames = new[] { "AllowHtml", "AllowHtmlAttribute" };

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AllowHtmlAttributeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AllowHtmlAttributeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AllowHtmlAttributeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeSyntax attribute)
            {
                return;
            }

            // Get the attribute's simple name
            var name = attribute.Name.ToString();
            var simpleNameStart = name.LastIndexOf('.');
            if (simpleNameStart > 0)
            {
                name = name.Substring(simpleNameStart + 1);
            }

            // If the attribute isn't [AllowHtml], bail out
            if (!DisallowedNames.Contains(name, StringComparer.Ordinal))
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't System.Web.Mvc.AllowHtmlAttribute, then bail out
            var attrNameSymbol = context.SemanticModel.GetTypeInfo(attribute.Name).Type;
            if (attrNameSymbol is INamedTypeSymbol symbol
                && attrNameSymbol is not IErrorTypeSymbol
                && !symbol.ToString().Equals(AllowHtmlAttributeName, StringComparison.Ordinal))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, attribute.GetLocation(), attribute.Name.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
