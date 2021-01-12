using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AspNetMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM009 CodeFix Provider")]
    public class HelperResultCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HelperResultTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HelperResultAnalyzer.DiagnosticId);
    }
}
