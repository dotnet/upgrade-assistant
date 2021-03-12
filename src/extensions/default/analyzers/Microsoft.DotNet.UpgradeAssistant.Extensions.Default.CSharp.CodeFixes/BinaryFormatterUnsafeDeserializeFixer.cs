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
    public sealed class BinaryFormatterUnsafeDeserializeFixer : CodeFixProvider
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
            if (node is not MemberAccessExpressionSyntax)
            {
                // stop processing - the code fixer only fixes member invocations of UnsafeDeserialize
                return document;
            }

            var memberExpression = (MemberAccessExpressionSyntax)node;

            if (memberExpression.Expression is IdentifierNameSyntax)
            {
                return await ProcessReplacementSyntax($"{((IdentifierNameSyntax)memberExpression.Expression).Identifier.ValueText}").ConfigureAwait(false);
            }
            else if (memberExpression!.Expression is ObjectCreationExpressionSyntax)
            {
                return await ProcessReplacementSyntax(((ObjectCreationExpressionSyntax)memberExpression.Expression).ToString()).ConfigureAwait(false);
            }
            else
            {
                // stop processing - the code fixer must be able to find an identifier on which the UnsafeDeserialize method was invoked
                return document;
            }

            async Task<Document> ProcessReplacementSyntax(string replacementText)
            {
                var replacementSyntax = ParseExpression($"{replacementText}.Deserialize")
                    .WithTriviaFrom(node);
                var slnEditor = new SolutionEditor(document.Project.Solution);
                var docEditor = await slnEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
                var docRoot = (CompilationUnitSyntax)docEditor.OriginalRoot;
                docRoot = docRoot.ReplaceNode(node, replacementSyntax);

                docEditor.ReplaceNode(docEditor.OriginalRoot, docRoot);

                return docEditor.GetChangedDocument();
            }
        }
    }
}
