using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HtmlStringAnalyzer : IdentifierMigrationAnalyzer
    {
        public const string DiagnosticId = "AM0002";
        private const string Category = "Migration";

        public override IEnumerable<IdentifierMapping> IdentifierMappings => new[]
        {
            new IdentifierMapping("System.Web.HtmlString", "Microsoft.AspNetCore.Html.HtmlString"),
            new IdentifierMapping("System.Web.IHtmlString", "Microsoft.AspNetCore.Html.HtmlString"),
            new IdentifierMapping("System.Web.Mvc.MvcHtmlString", "Microsoft.AspNetCore.Html.HtmlString")
        };

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HtmlStringTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HtmlStringMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HtmlStringDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        protected override Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string> properties, params object[] messageArgs) => Diagnostic.Create(Rule, location, properties, messageArgs);
    }
}
