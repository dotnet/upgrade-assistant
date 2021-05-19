// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public static class SyntaxNodeHelpers
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

            return node.IsKind(CodeAnalysis.CSharp.SyntaxKind.QualifiedName)
            || node.IsKind(CodeAnalysis.VisualBasic.SyntaxKind.QualifiedName);
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

            return node.IsKind(CodeAnalysis.CSharp.SyntaxKind.IdentifierName)
            || node.IsKind(CodeAnalysis.VisualBasic.SyntaxKind.IdentifierName);
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

            return node.IsKind(CodeAnalysis.CSharp.SyntaxKind.BaseList)
            || node.IsKind(CodeAnalysis.CSharp.SyntaxKind.SimpleBaseType)
            || node.IsKind(CodeAnalysis.VisualBasic.SyntaxKind.InheritsStatement);
        }
    }
}
