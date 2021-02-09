using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM002 CodeFix Provider")]
    public class HtmlStringCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HtmlStringTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HtmlStringAnalyzer.DiagnosticId);
    }
}
