﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;
using Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM003 CodeFix Provider")]
    public class ResultTypeCodeFixer : IdentifierMigrationCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.ResultTypeTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ResultTypeAnalyzer.DiagnosticId);
    }
}
