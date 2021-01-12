using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM007 CodeFix Provider")]
    public class HtmlHelperCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HtmlHelperTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HtmlHelperAnalyzer.DiagnosticId);
    }
}
