// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ApplicableComponents(ProjectComponents.Web)]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA007 CodeFix Provider")]
    public class HtmlHelperCodeFixer : IdentifierUpgradeCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.HtmlHelperTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HtmlHelperAnalyzer.DiagnosticId);
    }
}
