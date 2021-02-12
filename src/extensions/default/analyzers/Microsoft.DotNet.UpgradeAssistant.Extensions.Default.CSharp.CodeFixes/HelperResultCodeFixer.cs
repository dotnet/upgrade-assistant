using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;
using Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM009 CodeFix Provider")]
    public class HelperResultCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HelperResultTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HelperResultAnalyzer.DiagnosticId);
    }
}
