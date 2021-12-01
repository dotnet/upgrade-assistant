// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    public abstract class IdentifierUpgradeCodeFixer : CodeFixProvider
    {
        public abstract string CodeFixTitle { get; }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root of the document to be fixed
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            // The node is found by location, so it's possible `FindNode` will find a parent with the same span.
            // To get the correct node, find the first name or MAE in the given span.
            var node = root.FindNode(context.Span)
                .DescendantNodesAndSelf(n => true)
                .FirstOrDefault(n => n.Span.Equals(context.Span)
                    && (n.IsNameSyntax() || n.IsMemberAccessExpression()));

            if (node is null)
            {
                return;
            }

            // Get the diagnostic to be fixed (the new identifier will be a property of the diagnostic)
            var diagnostic = context.Diagnostics.FirstOrDefault();

            if (diagnostic is null)
            {
                return;
            }

            // Get the new identifier name and register the code fix action
            if (diagnostic.Properties.TryGetValue(TypeUpgradeAnalyzer.NewIdentifierKey, out var property) && property is not null)
            {
                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixTitle,
                        cancellationToken => UpdateIdentifierTypeAsync(context.Document, node, property, cancellationToken),
                        CodeFixTitle),
                    context.Diagnostics);
            }
        }

        /// <summary>
        /// Replaces a syntax node with a new fully qualified name or member accession expression from a given string.
        /// </summary>
        /// <param name="document">The document to update.</param>
        /// <param name="node">The syntax node to be replaced.</param>
        /// <param name="newIdentifier">A string representation of the new identifier (name of member access expression) to be used in the document.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An updated document with the given node replaced with the new identifier.</returns>
        private static async Task<Document> UpdateIdentifierTypeAsync(Document document, SyntaxNode node, string newIdentifier, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create new identifier
            var updatedNode = GetUpdatedNode(node, editor.Generator, newIdentifier)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation)
                .WithTriviaFrom(node);

            editor.ReplaceNode(node, updatedNode);

            var updatedDocument = editor.GetChangedDocument();

            // Add using declaration if needed
            updatedDocument = await ImportAdder.AddImportsAsync(updatedDocument, Simplifier.AddImportsAnnotation, null, cancellationToken).ConfigureAwait(false);

            // Simplify the call, if possible
            updatedDocument = await Simplifier.ReduceAsync(updatedDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);

            return updatedDocument;
        }

        private static SyntaxNode GetUpdatedNode(SyntaxNode node, SyntaxGenerator generator, string name)
        {
            return node switch
            {
                // Many usages (constructing a type, casting to a type, etc.) will use a qualified name syntax
                // to refer to the type.
                var nameSyntax when nameSyntax.IsNameSyntax() => QualifiedNameBuilder.BuildQualifiedNameSyntax(generator, name),

                // Accessing a static member of a type will use a member access expression to refer to the type.
                var maeSyntax when maeSyntax.IsMemberAccessExpression() => QualifiedNameBuilder.BuildMemberAccessExpression(generator, name),

                // Because the node is retrieved by location, it may sometimes be necessary to check children.
                _ => GetUpdatedNode(node.ChildNodes().FirstOrDefault(), generator, name)
            };
        }
    }
}
