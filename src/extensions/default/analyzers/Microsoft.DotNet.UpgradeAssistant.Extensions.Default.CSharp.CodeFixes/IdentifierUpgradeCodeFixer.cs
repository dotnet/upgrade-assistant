// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    public abstract class IdentifierUpgradeCodeFixer : CodeFixProvider
    {
        public abstract string CodeFixTitle { get; }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
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
                    && (n is CSSyntax.NameSyntax
                    || n is VBSyntax.NameSyntax
                    || n is CSSyntax.MemberAccessExpressionSyntax
                    || n is VBSyntax.MemberAccessExpressionSyntax));

            if (node is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            if (diagnostic is null)
            {
                return;
            }

            if (diagnostic.Properties.TryGetValue(IdentifierUpgradeAnalyzer.NewIdentifierKey, out var property) && property is not null)
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

        private static async Task<Document> UpdateIdentifierTypeAsync(Document document, SyntaxNode node, string newIdentifier, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (documentRoot is null)
            {
                return document;
            }

            // Create new identifier
            var updatedNode = GetUpdatedNode(node, newIdentifier);

            if (updatedNode is null)
            {
                return document;
            }

            var updatedRoot = documentRoot.ReplaceNode(node, updatedNode);
            var updatedDocument = document.WithSyntaxRoot(updatedRoot);

            // Add using declaration if needed
            updatedDocument = await ImportAdder.AddImportsAsync(updatedDocument, Simplifier.AddImportsAnnotation, null, cancellationToken).ConfigureAwait(false);
            updatedDocument = await Simplifier.ReduceAsync(updatedDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);

            return updatedDocument;
        }

        private static SyntaxNode? GetUpdatedNode(SyntaxNode node, string newIdentifier) =>
            node is null
                ? null
                : node switch
                {
                    // Many usages (constructing a type, casting to a type, etc.) will use a qualified name syntax
                    // to refer to the type.
                    CSSyntax.NameSyntax csNameSyntax => CreateQualifiedName(newIdentifier, csNameSyntax),
                    VBSyntax.NameSyntax vbNameSyntax => CreateQualifiedName(newIdentifier, vbNameSyntax),

                    // Accessing a static member of a type will use a member access expression to refer to the type.
                    CSSyntax.MemberAccessExpressionSyntax csMAE => CreateMemberAccessExpression(newIdentifier, csMAE),
                    VBSyntax.MemberAccessExpressionSyntax vbMAE => CreateMemberAccessExpression(newIdentifier, vbMAE),

                    // Using a type as a base type uses a base type syntax.
          //          CSSyntax.BaseTypeSyntax csBaseType => CreateBaseType(newIdentifier, csBaseType),

                    // Because the node is retrieved by location, it may sometimes be necessary to check children.
                    _ => GetUpdatedNode(node.ChildNodes().FirstOrDefault(), newIdentifier)
                };

        private static CSSyntax.QualifiedNameSyntax CreateQualifiedName(string name, CSSyntax.TypeSyntax node) =>
            (CSSyntax.QualifiedNameSyntax)CS.SyntaxFactory.ParseName(name)
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

        private static VBSyntax.QualifiedNameSyntax CreateQualifiedName(string name, VBSyntax.TypeSyntax node) =>
            (VBSyntax.QualifiedNameSyntax)VB.SyntaxFactory.ParseName(name)
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

        private static CSSyntax.MemberAccessExpressionSyntax CreateMemberAccessExpression(string name, CSSyntax.MemberAccessExpressionSyntax node) =>
            (CSSyntax.MemberAccessExpressionSyntax)CS.SyntaxFactory.ParseExpression(name)
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

        private static VBSyntax.MemberAccessExpressionSyntax CreateMemberAccessExpression(string name, VBSyntax.MemberAccessExpressionSyntax node) =>
            (VBSyntax.MemberAccessExpressionSyntax)VB.SyntaxFactory.ParseExpression(name)
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

        private static CSSyntax.BaseTypeSyntax CreateBaseType(string name, CSSyntax.BaseTypeSyntax node) =>
            node.WithType(CreateQualifiedName(name, node.Type))
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

    }
}
