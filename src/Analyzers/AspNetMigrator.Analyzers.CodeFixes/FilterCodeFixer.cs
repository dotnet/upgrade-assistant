using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM004 CodeFix Provider")]
    public class FilterCodeFixer : IdentiferMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.FilterTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(FilterAnalyzer.DiagnosticId);
    }
}
