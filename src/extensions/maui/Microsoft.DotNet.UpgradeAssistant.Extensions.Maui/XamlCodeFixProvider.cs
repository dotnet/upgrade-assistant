// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Xaml;
using Microsoft.VisualStudio.DesignTools.Xaml.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.CodeAnalysis.Shared;
using Microsoft.VisualStudio.DesignTools.Xaml.LanguageService.Semantics;
using System.Text;
using Microsoft.CodeAnalysis.Editing;

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
                    CodeAction act = CodeAction.Create(
                        diag.Descriptor.Title.ToString(),
                        t => FixXaml(context, diag, t),
                        nameof(Resources.XamlAnalyzerTitle));
                    context.RegisterCodeFix(act, diag);
                }
            }

            return Task.CompletedTask;
        }

        private async Task<Solution> FixXaml(CodeFixContext context, Diagnostic diag, CancellationToken token)
        {
            TextDocument document = context.TextDocument;
            string allChanges = diag.Properties["Changes"] ?? throw new InvalidOperationException();
            if (!Change.TryParse(allChanges, out IReadOnlyList<Change> changes) ||
                changes.Count == 0)
            {
                return document.Project.Solution;
            }

            SourceText sourceText = await context.TextDocument.GetTextAsync(token).ConfigureAwait(false);
            var text = sourceText.ToString();
            StringBuilder builder = new StringBuilder(text.Length);

            int start = 0;
            foreach (var change in changes)
            {
                int end = change.Offset;
                string left = text.Substring(start, end - start);
                builder.Append(left);
                builder.Append(change.Text);
                start = end + change.Length;
            }

            builder.Append(text.Substring(start));
            text = builder.ToString();
            var newSourceText = SourceText.From(text, sourceText.Encoding);
            Solution solution = document.Project.Solution.WithAdditionalDocumentText(document.Id, newSourceText);

            // TODO: figure the right way of saving changed *.xaml file
            System.IO.File.WriteAllText(document.FilePath, text, sourceText.Encoding);

            return solution;
        }
    }
}
