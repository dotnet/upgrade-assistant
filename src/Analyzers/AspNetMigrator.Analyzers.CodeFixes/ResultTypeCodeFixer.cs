using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM003 CodeFix Provider")]
    public class ResultTypeCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.ResultTypeTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ResultTypeAnalyzer.DiagnosticId);
    }
}
