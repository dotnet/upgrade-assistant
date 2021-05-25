// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "UA0012 CodeFix Provider")]
    public sealed class BinaryFormatterUnsafeDeserializeCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BinaryFormatterUnsafeDeserializeAnalyzer.DiagnosticId);

        /// <summary>
        /// references to "UnsafeDeserialize" should be replaced with "UnsafeDeserialize" when the 2nd param to "UnsafeDeserialize" is null.
        /// </summary>
        public const string ReplacementMethod = "Deserialize";

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
            if (arguments.Count() != 2 || !lastArgument.IsNullLiteralExpression())
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

        /// <summary>
        /// Fixes SimpleMemberAccessExpressions e.g.
        /// - new BinaryFormatter().UnsafeDeserialize(someVar, null)
        /// - formatterVar.UnsafeDeserialize(anotherVar, null)
        /// By replacing "UnsafeDeserialize" because it does not exist on .NET Core.
        /// </summary>
        /// <param name="document">a code document.</param>
        /// <param name="node">expecting a simpleMemberAccessExpression of CS or VB syntax.</param>
        /// <param name="cancellationToken">the cancellationToken.</param>
        /// <returns>A document with the corrected method call.</returns>
        private static async Task<Document> ReplaceUnsafeDeserializationAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (documentRoot is null)
            {
                return document;
            }

            // use the document to build a language aware code generator
            var generator = SyntaxGenerator.GetGenerator(document);

            // the code fixer is only registered to handle code that derives from an invocationExpression
            if (!GeneralInvocationExpression.TryParse(node.Parent!, out var expression))
            {
                return document;
            }

            // list of existing arguments
            var args = expression.GetArguments();

            // the codefixer should only be triggered when we are replacing "UnsafeDeserialize"
            // so we can assume that the node has at least one descendant when invoking .First()
            var newMemberAccessExpression = generator.MemberAccessExpression(node.DescendantNodes().First(), ReplacementMethod);

            // create a new invocation
            var invocation = generator.InvocationExpression(newMemberAccessExpression, args.First());

            // replace the invocationExpression with the new one we just created
            var newDocRoot = documentRoot.ReplaceNode(node.Parent!, invocation);

            return document.WithSyntaxRoot(newDocRoot);
        }
    }
}
