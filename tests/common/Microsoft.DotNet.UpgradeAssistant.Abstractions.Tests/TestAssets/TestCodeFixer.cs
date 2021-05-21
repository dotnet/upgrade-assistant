// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests.TestAssets
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.FSharp, Name = "UHOH1 CodeFix Provider")]
    public class TestCodeFixer : IdentifierUpgradeCodeFixer
    {
        public override string CodeFixTitle => "Fake Title";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("UHOH1 CodeFix Provider");
    }
}
