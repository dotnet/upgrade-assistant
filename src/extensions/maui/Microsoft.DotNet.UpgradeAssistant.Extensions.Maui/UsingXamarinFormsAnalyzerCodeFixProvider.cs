// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    [ApplicableComponents(ProjectComponents.Maui)]
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class UsingXamarinFormsAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UsingXamarinFormsAnalyzerAnalyzer.DiagnosticId);

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

            // Register the appropriate code action that will invoke the fix
            switch (node.RawKind)
            {
                case (int)SyntaxKind.UsingDirective:
                case (int)SyntaxKind.UsingStatement:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            Resources.UsingXamarinFormsTitle,
                            cancellationToken => ReplaceUsingStatementAsync(context.Document, node, cancellationToken),
                            nameof(Resources.UsingXamarinFormsTitle)),
                        context.Diagnostics);
                    break;
                case (int)SyntaxKind.QualifiedName:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            Resources.NamespaceXamarinFormsTitle,
                            cancellationToken => RemoveNamespaceQualifierAsync(context.Document, node, cancellationToken),
                            nameof(Resources.NamespaceXamarinFormsTitle)),
                        context.Diagnostics);
                    break;
            }
        }

        private static async Task<Document> ReplaceUsingStatementAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var documentRoot = (CompilationUnitSyntax)editor.OriginalRoot;
            documentRoot = documentRoot.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            documentRoot = documentRoot?.AddUsingIfMissing("Microsoft.Maui");
            documentRoot = documentRoot?.AddUsingIfMissing("Microsoft.Maui.Controls");

            if (documentRoot is not null)
            {
                editor.ReplaceNode(editor.OriginalRoot, documentRoot);
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> RemoveNamespaceQualifierAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (node.Parent is not null)
            {
                editor.ReplaceNode(node.Parent, node.Parent.ChildNodes().Last());
            }

            return editor.GetChangedDocument();
        }
    }
}
