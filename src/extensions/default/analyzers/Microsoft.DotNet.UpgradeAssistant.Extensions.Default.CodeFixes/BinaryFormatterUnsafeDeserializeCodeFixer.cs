// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "UA0012 CodeFix Provider")]
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

            if (node is null || node.Parent is null)
            {
                return;
            }

            if (!GeneralInvocationExpression.TryParse(node.Parent, out var invocationExpression))
            {
                return;
            }

            var arguments = invocationExpression.GetArguments();
            var lastArgument = arguments.Last();
            if (arguments.Count() != 2 || !NullLiteralExpression(lastArgument))
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

        private static bool NullLiteralExpression(string lastArgument)
        {
            return "NullLiteralExpression".Equals(lastArgument, StringComparison.OrdinalIgnoreCase)
                || "NothingLiteralExpression".Equals(lastArgument, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<Document> ReplaceUnsafeDeserializationAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node.Parent is null)
            {
                return document;
            }

            var memberExpression = node as MemberAccessExpressionSyntax;
            if (memberExpression is null)
            {
                return document;
            }

            var project = document.Project;
            var slnEditor = new SolutionEditor(project.Solution);

            var docEditor = await slnEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            var docRoot = docEditor.OriginalRoot;

            ReplaceMethodCall(memberExpression, docEditor);
            DropNullParameter(memberExpression, docEditor);

            docEditor.ReplaceNode(docEditor.OriginalRoot, docRoot);
            return docEditor.GetChangedDocument();
        }

        private static void ReplaceMethodCall(MemberAccessExpressionSyntax? memberExpression, DocumentEditor docEditor)
        {
            if (memberExpression is null)
            {
                return;
            }

            var parsedExpression = ParseExpression("Deserialize");
            var identifierExpression = memberExpression.DescendantNodes().OfType<IdentifierNameSyntax>().Last();
            docEditor.ReplaceNode(identifierExpression, parsedExpression);
        }

        private static void DropNullParameter(MemberAccessExpressionSyntax? memberExpression, DocumentEditor docEditor)
        {
            if (memberExpression is null || memberExpression.Parent is null)
            {
                return;
            }

            var argumentListSyntaxOriginal = memberExpression.Parent.DescendantNodes().OfType<ArgumentListSyntax>().Last();
            var modified = argumentListSyntaxOriginal.WithArguments(argumentListSyntaxOriginal.Arguments.RemoveAt(1));
            docEditor.ReplaceNode(argumentListSyntaxOriginal, modified);
        }
    }
}
