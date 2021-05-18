// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Handles language aware selection of QualifiedNameSyntax or IdentifierNameSyntaxNode from current context.
        /// </summary>
        /// <param name="importOrBaseListSyntax">Shuold be an ImportStatementSyntax for VB or a BaseListSyntax for CS.</param>
        /// <returns>Null, QualifiedNameSyntaxNode, or IdentifierNameSyntaxNode.</returns>
        public static SyntaxNode? GetSyntaxIdentifierForBaseType(this SyntaxNode importOrBaseListSyntax)
        {
            if (importOrBaseListSyntax is null)
            {
                return null;
            }

            if (importOrBaseListSyntax.IsQualifiedName() || importOrBaseListSyntax.IsIdentifierName())
            {
                return importOrBaseListSyntax;
            }
            else if (!importOrBaseListSyntax.IsBaseTypeSyntax())
            {
                return null;
            }

            var baseTypeNode = importOrBaseListSyntax.DescendantNodes(descendIntoChildren: node => true)
                .FirstOrDefault(node => node.IsQualifiedName() || node.IsIdentifierName());

            return baseTypeNode;
        }

        /// <summary>
        /// A language agnostic specification that checks if a node is a QualifiedName.
        /// </summary>
        /// <param name="node">any SyntaxNode.</param>
        /// <returns>True if the node IsKind(SyntaxKind.QualifiedName).</returns>
        public static bool IsQualifiedName(this SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.IsKind(CS.SyntaxKind.QualifiedName)
            || node.IsKind(VB.SyntaxKind.QualifiedName);
        }

        /// <summary>
        /// A language agnostic specification that checks if a node is a IdentifierName.
        /// </summary>
        /// <param name="node">any SyntaxNode.</param>
        /// <returns>True if the node IsKind(SyntaxKind.IdentifierName).</returns>
        public static bool IsIdentifierName(this SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.IsKind(CS.SyntaxKind.IdentifierName)
            || node.IsKind(VB.SyntaxKind.IdentifierName);
        }

        /// <summary>
        /// A language agnostic specification that checks if a node is one of
        /// SyntaxKind.BaseList, SyntaxKind.SimpleBaseType, or SyntaxKind.InheritsStatement.
        /// </summary>
        /// <param name="node">any SyntaxNode.</param>
        /// <returns>True if the node IsKind of SyntaxKind.BaseList,
        /// SyntaxKind.SimpleBaseType, or SyntaxKind.InheritsStatement.</returns>
        public static bool IsBaseTypeSyntax(this SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.IsKind(CS.SyntaxKind.BaseList)
            || node.IsKind(CS.SyntaxKind.SimpleBaseType)
            || node.IsKind(VB.SyntaxKind.InheritsStatement);
        }

        /// <summary>
        /// Finds the fully qualified name syntax or member access expression syntax (if any)
        /// that a node is part of and returns that larger, fully qualified syntax node.
        /// </summary>
        /// <param name="node">
        /// The identifier syntax node to find a fully qualified ancestor for. This should be of type
        /// Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax or Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax.
        /// </param>
        /// <returns>
        /// Returns the qualified name syntax or member access expression syntax containing the provided
        /// name syntax. If the node is not part of a larger qualified name, the input node will be returned.
        /// </returns>
        public static SyntaxNode GetQualifiedName(this SyntaxNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // If the node is part of a qualified name, we want to get the full qualified name
            while (node.Parent is CSSyntax.NameSyntax || node.Parent is VBSyntax.NameSyntax)
            {
                node = node.Parent;
            }

            // If the node is part of a member access expression (a static member access, for example), then the
            // qualified name will be a member access expression rather than a name syntax.
            if ((node.Parent is CSSyntax.MemberAccessExpressionSyntax csMAE && csMAE.Name.ToString().Equals(node.ToString(), StringComparison.Ordinal))
                || (node.Parent is VBSyntax.MemberAccessExpressionSyntax vbMAE && vbMAE.Name.ToString().Equals(node.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                node = node.Parent;
            }

            return node;
        }

        /// <summary>
        /// Gets a method declared in a given syntax node.
        /// </summary>
        /// <typeparam name="T">The type of method declaration syntax node to search for.</typeparam>
        /// <param name="node">The syntax node to find the method declaration in.</param>
        /// <param name="methodName">The name of the method to return.</param>
        /// <param name="requiredParameterTypes">An optional list of parameter types that the method must accept.</param>
        /// <returns>The first method declaration in the syntax node with the given name and parameter types, or null if no such method declaration exists.</returns>
        public static T? GetMethodDeclaration<T>(this SyntaxNode node, string methodName, params string[] requiredParameterTypes)
            where T : SyntaxNode =>
            node?.DescendantNodes()
                .OfType<T>()
                .FirstOrDefault(m =>
                    (m is CSSyntax.MethodDeclarationSyntax csM &&
                    csM.Identifier.ToString().Equals(methodName, StringComparison.Ordinal) &&
                    requiredParameterTypes.All(req => csM.ParameterList.Parameters.Any(p => string.Equals(p.Type?.ToString(), req, StringComparison.Ordinal))))
                || (m is VBSyntax.MethodStatementSyntax vbM &&
                    vbM.Identifier.ToString().Equals(methodName, StringComparison.Ordinal) &&
                    requiredParameterTypes.All(req => vbM.ParameterList.Parameters.Any(p => string.Equals(p.AsClause?.Type?.ToString(), req, StringComparison.Ordinal)))));

        /// <summary>
        /// Applies whitespace trivia and new line trivia from another syntax node to this one.
        /// </summary>
        /// <typeparam name="T">The type of syntax node to be updated.</typeparam>
        /// <param name="statement">The syntax node to update.</param>
        /// <param name="otherStatement">The syntax node to copy trivia from.</param>
        /// <returns>The original syntax node updated with the other syntax's whitespace and new line trivia.</returns>
        public static T WithWhitespaceTriviaFrom<T>(this T statement, SyntaxNode otherStatement)
            where T : SyntaxNode
        {
            return statement
                .WithLeadingTrivia(otherStatement?.GetLeadingTrivia().Where(IsWhitespaceTrivia) ?? SyntaxTriviaList.Empty)
                .WithTrailingTrivia(otherStatement?.GetTrailingTrivia().Where(IsWhitespaceTrivia) ?? SyntaxTriviaList.Empty);

            static bool IsWhitespaceTrivia(SyntaxTrivia trivia) =>
                CSharpExtensions.IsKind(trivia, CS.SyntaxKind.EndOfLineTrivia)
                || CSharpExtensions.IsKind(trivia, CS.SyntaxKind.WhitespaceTrivia)
                || VisualBasicExtensions.IsKind(trivia, VB.SyntaxKind.EndOfLineTrivia)
                || VisualBasicExtensions.IsKind(trivia, VB.SyntaxKind.WhitespaceTrivia);
        }

        /// <summary>
        /// Adds a using directive for a given namespace to the document root only if the directive is not already present.
        /// </summary>
        /// <param name="documentRoot">The document to add the directive to.</param>
        /// <param name="namespaceName">The namespace to reference with the using directive.</param>
        /// <returns>An updated document root with the specific using directive.</returns>
        public static CSSyntax.CompilationUnitSyntax AddUsingIfMissing(this CSSyntax.CompilationUnitSyntax documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            var anyUsings = documentRoot.Usings.Any(u => u.Name.ToString().Equals(namespaceName, StringComparison.Ordinal));
            var usingDirective = CS.SyntaxFactory.UsingDirective(CS.SyntaxFactory.ParseName(namespaceName).WithLeadingTrivia(CS.SyntaxFactory.Whitespace(" ")));
            var result = anyUsings ? documentRoot : documentRoot.AddUsings(usingDirective);

            return result;
        }
    }
}
