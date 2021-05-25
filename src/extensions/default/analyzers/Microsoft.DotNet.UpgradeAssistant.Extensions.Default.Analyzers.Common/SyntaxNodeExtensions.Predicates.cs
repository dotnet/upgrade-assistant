// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public static partial class SyntaxNodeExtensions
    {
        public static bool IsCSharp(this SyntaxNode node) => node?.Language == LanguageNames.CSharp;

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

        public static bool IsInvocationExpression(this SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.IsKind(CS.SyntaxKind.InvocationExpression)
                || node.IsKind(VB.SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Determines whether a node is a MemberAccessExpressionSyntax (either C# or VB).
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <returns>True if the node derives from Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax or Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax, false otherwise.</returns>
        public static bool IsMemberAccessExpression(this SyntaxNode node) => node is CSSyntax.MemberAccessExpressionSyntax || node is VBSyntax.MemberAccessExpressionSyntax;

        /// <summary>
        /// Determines whether a node is a NameSyntax (either C# or VB).
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <returns>True if the node derives from Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax or Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax, false otherwise.</returns>
        public static bool IsNameSyntax(this SyntaxNode node) => node is CSSyntax.NameSyntax || node is VBSyntax.NameSyntax;

        public static bool IsNullLiteralExpression(this SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.IsKind(CS.SyntaxKind.NullLiteralExpression)
                || node.IsKind(VB.SyntaxKind.NothingLiteralExpression);
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

        public static bool IsVisualBasic(this SyntaxNode node) => node?.Language == LanguageNames.VisualBasic;
    }
}
