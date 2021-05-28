// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "UA0002 CodeFix Provider")]
    public class TypeUpgradeCodeFixer : IdentifierUpgradeCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.TypeUpgradeTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TypeUpgradeAnalyzer.DiagnosticId);
    }
}
