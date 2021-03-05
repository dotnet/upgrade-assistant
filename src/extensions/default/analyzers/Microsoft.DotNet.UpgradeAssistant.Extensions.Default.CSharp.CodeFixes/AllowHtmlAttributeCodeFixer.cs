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
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ApplicableComponents(ProjectComponents.Web)]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA0010 CodeFix Provider")]
    public class AllowHtmlAttributeCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AllowHtmlAttributeAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
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
                    CodeFixResources.AllowHtmlAttributeTitle,
                    cancellationToken => RemoveNodeAsync(context.Document, node, cancellationToken),
                    nameof(CodeFixResources.AllowHtmlAttributeTitle)),
                context.Diagnostics);
        }

        private static async Task<Document> RemoveNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Remove the attribute or, if it's the only attribute in the attribute list, remove the attribute list
            if (node.Parent is AttributeListSyntax list && list.Attributes.Count == 1)
            {
                // We want to remove trivia with the attribute list, but we want to *keep* leading end of line trivia
                // so that we don't remove blank lines before the property that had the attribute list on it.
                // To do that, we first remove any leading trivial except new lines and then remove the node keeping
                // leading trivia.
                var trimmedList = list.WithLeadingTrivia(list.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia)));
                editor.ReplaceNode(list, trimmedList);
                editor.RemoveNode(trimmedList, SyntaxRemoveOptions.KeepLeadingTrivia);
            }
            else
            {
                editor.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return editor.GetChangedDocument();
        }
    }
}
