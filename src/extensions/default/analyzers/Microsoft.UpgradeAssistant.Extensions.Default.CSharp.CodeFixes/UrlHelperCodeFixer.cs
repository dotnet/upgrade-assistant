using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM008 CodeFix Provider")]
    public class UrlHelperCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.UrlHelperTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UrlHelperAnalyzer.DiagnosticId);
    }
}
