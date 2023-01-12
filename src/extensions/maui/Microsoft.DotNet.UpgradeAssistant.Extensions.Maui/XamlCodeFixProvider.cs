// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    [ApplicableComponents(ProjectComponents.Maui)]
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class XamlCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(XamlAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diag in context.Diagnostics)
            {
                if (diag.Id == XamlAnalyzer.DiagnosticId)
                {
                    CodeAction act = CodeAction.Create(Resources.XamlAnalyzerTitle, t => ReplaceXaml(context, diag, t), nameof(Resources.XamlAnalyzerTitle));
                    context.RegisterCodeFix(act, diag);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task<Solution> ReplaceXaml(CodeFixContext context, Diagnostic diag, CancellationToken token)
        {
            var newText = diag.Properties[XamlAnalyzer.NewText] ?? throw new InvalidOperationException();
            TextDocument document = context.TextDocument;
            SourceText sourceText = await context.TextDocument.GetTextAsync(token).ConfigureAwait(false);
            var newSourceText = SourceText.From(newText, sourceText.Encoding);
            Solution solution = document.Project.Solution.WithAdditionalDocumentText(document.Id, newSourceText);

            return solution;
        }
    }
}
