// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "Using Microsoft.Maui.Essentials code fixer")]
    public class UsingXamarinEssentialsAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UsingXamarinEssentialsAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var node = root.FindNode(context.Span);

            if (node is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.UsingXamarinEssentialsTitle,
                    cancellationToken => ReplaceNodeAsync(context.Document, node, cancellationToken),
                    nameof(Resources.UsingXamarinEssentialsTitle)),
                context.Diagnostics);
        }

        private static async Task<Document> ReplaceNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var documentRoot = (CompilationUnitSyntax)editor.OriginalRoot;
            documentRoot = documentRoot.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

            if (documentRoot is not null)
            {
                editor.ReplaceNode(editor.OriginalRoot, documentRoot);
            }

            return editor.GetChangedDocument();
        }
    }
}
