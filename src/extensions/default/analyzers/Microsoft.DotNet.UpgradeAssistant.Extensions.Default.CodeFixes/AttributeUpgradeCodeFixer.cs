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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA0010 CodeFix Provider")]
    public class AttributeUpgradeCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AttributeUpgradeAnalyzer.DiagnosticId);

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

            // Get the diagnostic to be fixed (the new attribute type will be a property of the diagnostic)
            var diagnostic = context.Diagnostics.FirstOrDefault();

            if (diagnostic is null)
            {
                return;
            }

            var newType = diagnostic.Properties.GetValueOrDefault(AttributeUpgradeAnalyzer.NewTypeKey);

            if (newType is null)
            {
                // Register a code action that will remove the attribute.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AttributeRemoveTitle,
                        cancellationToken => RemoveAttributeAsync(context.Document, node, cancellationToken),
                        nameof(CodeFixResources.AttributeRemoveTitle)),
                    context.Diagnostics);
            }
            else
            {
                // Register a code action that will replace the attribute.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AttributeRemoveTitle,
                        cancellationToken => ReplaceAttributeAsync(context.Document, node, newType, cancellationToken),
                        nameof(CodeFixResources.AttributeRemoveTitle)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> RemoveAttributeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Remove the attribute or, if it's the only attribute in the attribute list, remove the attribute list
            if ((node.Parent is CSSyntax.AttributeListSyntax csList && csList.Attributes.Count == 1)
                || (node.Parent is VBSyntax.AttributeListSyntax vbList && vbList.Attributes.Count == 1))
            {
                // We want to remove trivia with the attribute list, but we want to *keep* leading end of line trivia
                // so that we don't remove blank lines before the property that had the attribute list on it.
                // To do that, we first remove any leading trivia except new lines and then remove the node keeping
                // leading trivia.
                var trimmedList = node.Parent!.WithLeadingTrivia(node.Parent!.GetLeadingTrivia().Where(t => t.IsKind(CS.SyntaxKind.EndOfLineTrivia) || t.IsKind(VB.SyntaxKind.EndOfLineTrivia)));
                editor.ReplaceNode(node.Parent, trimmedList);
                editor.RemoveNode(trimmedList, SyntaxRemoveOptions.KeepLeadingTrivia);
            }
            else
            {
                editor.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ReplaceAttributeAsync(Document document, SyntaxNode node, string attributeType, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            // Create an updated Attribute node
            var updatedNode = node switch
            {
                CSSyntax.AttributeSyntax csAttribute => (SyntaxNode)csAttribute.WithName((CSSyntax.NameSyntax)QualifiedNameBuilder.BuildQualifiedNameSyntax(generator, attributeType)),
                VBSyntax.AttributeSyntax vbAttribute => vbAttribute.WithName((VBSyntax.NameSyntax)QualifiedNameBuilder.BuildQualifiedNameSyntax(generator, attributeType)),
                _ => throw new InvalidOperationException($"Unexpected syntax node type: {node.GetType().FullName}")
            };
            updatedNode = updatedNode.WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

            // Get the document root
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (documentRoot is null)
            {
                return document;
            }

            // Replace the node
            var updatedRoot = documentRoot.ReplaceNode(node, updatedNode);
            var updatedDocument = document.WithSyntaxRoot(updatedRoot);

            // Add using declaration if needed
            updatedDocument = await ImportAdder.AddImportsAsync(updatedDocument, Simplifier.AddImportsAnnotation, null, cancellationToken).ConfigureAwait(false);

            // Simplify the call, if possible
            updatedDocument = await Simplifier.ReduceAsync(updatedDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);

            return updatedDocument;
        }
    }
}
