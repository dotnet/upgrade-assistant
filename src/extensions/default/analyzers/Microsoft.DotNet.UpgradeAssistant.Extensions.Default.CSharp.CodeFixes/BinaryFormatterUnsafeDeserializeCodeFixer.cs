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
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA0012 CodeFix Provider")]
    public sealed class BinaryFormatterUnsafeDeserializeCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BinaryFormatterUnsafeDeserializeAnalyzer.DiagnosticId);

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

            var node = root.FindNode(context.Span, false, true);

            if (node is null)
            {
                return;
            }

            var invocationExpression = node.Parent as InvocationExpressionSyntax;
            if (invocationExpression is null)
            {
                return;
            }

            var lastArg = invocationExpression.ArgumentList.Arguments.Last();
            if (!"null".Equals(lastArg.GetText().ToString(), System.StringComparison.Ordinal))
            {
                // UnsafeDeserialize accepts 2 parameters. This code fix only applies when the 2nd param is null
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.BinaryFormatterUnsafeDeserializeTitle,
                    cancellationToken => ReplaceUnsafeDeserializationAsync(context.Document, node, cancellationToken),
                    nameof(CodeFixResources.BinaryFormatterUnsafeDeserializeTitle)),
                context.Diagnostics);
        }

        private static async Task<Document> ReplaceUnsafeDeserializationAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node.Parent is null)
            {
                return document;
            }

            var project = document.Project;
            var slnEditor = new SolutionEditor(project.Solution);

            var docEditor = await slnEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            var docRoot = docEditor.OriginalRoot;

            var replacementExpression = node.Parent.ToFullString();
            replacementExpression = ReplaceMethodName(replacementExpression);
            replacementExpression = DropSecondParameter(replacementExpression);
            var replacementSyntax = ParseExpression(replacementExpression)
                    .WithTriviaFrom(node);
            docRoot = docRoot.ReplaceNode(node.Parent, replacementSyntax);

            docEditor.ReplaceNode(docEditor.OriginalRoot, docRoot);
            return docEditor.GetChangedDocument();

            string DropSecondParameter(string invocationExpression)
            {
                return invocationExpression.Substring(0, invocationExpression.IndexOf(",", System.StringComparison.Ordinal)) + ")";
            }

            string ReplaceMethodName(string invocationExpression)
            {
                return invocationExpression.Replace("UnsafeDeserialize", "Deserialize");
            }
        }
    }
}
