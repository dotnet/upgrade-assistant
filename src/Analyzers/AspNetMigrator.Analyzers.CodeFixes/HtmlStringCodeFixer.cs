using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM002 CodeFix Provider")]
    public class HtmlStringCodeFixer : IdentiferMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HtmlStringTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HtmlStringAnalyzer.DiagnosticId);
    }
}
