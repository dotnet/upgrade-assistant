using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;
using Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM008 CodeFix Provider")]
    public class UrlHelperCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.UrlHelperTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UrlHelperAnalyzer.DiagnosticId);
    }
}
