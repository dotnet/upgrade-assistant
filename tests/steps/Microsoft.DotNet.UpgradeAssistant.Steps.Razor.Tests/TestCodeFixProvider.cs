// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class TestCodeFixProvider : CodeFixProvider
    {
        private readonly ImmutableArray<string> _diagnosticIds;

        public TestCodeFixProvider(IEnumerable<string> diagnosticIds)
        {
            _diagnosticIds = ImmutableArray.CreateRange(diagnosticIds ?? throw new ArgumentNullException(nameof(diagnosticIds)));
        }

        public override ImmutableArray<string> FixableDiagnosticIds => _diagnosticIds;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(true);

            if (root is null)
            {
                return;
            }

            var node = root.FindNode(context.Span);

            if (node is null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Fix {diagnostic.Id}",
                        ct => ReplaceNodeAsync(context.Document, node, ct)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> ReplaceNodeAsync(Document document, SyntaxNode node, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            editor.ReplaceNode(node, node.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(" /* Test! */").AddRange(node.GetTrailingTrivia())));
            return editor.GetChangedDocument();
        }
    }
}
