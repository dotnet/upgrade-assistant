// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;

using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default
{
    public static partial class SyntaxNodeExtensions
    {
        public static SyntaxNode AddArgumentToInvocation(this SyntaxNode invocationNode, SyntaxNode argument)
        {
            if (invocationNode.IsVisualBasic())
            {
                var node = (VBSyntax.InvocationExpressionSyntax)invocationNode;
                return node.WithArgumentList(node.ArgumentList.AddArguments((VBSyntax.ArgumentSyntax)argument));
            }
            else if (invocationNode.IsCSharp())
            {
                var node = (CSSyntax.InvocationExpressionSyntax)invocationNode;
                return node.WithArgumentList(node.ArgumentList.AddArguments((CSSyntax.ArgumentSyntax)argument));
            }

            throw new NotImplementedException(Resources.UnknownLanguage);
        }

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
        /// Gets the simple name of an identifier syntax. Works for both C# and VB.
        /// </summary>
        /// <param name="node">The IdentifierNameSyntax node to get the simple name for.</param>
        /// <returns>The identifier's simple name.</returns>
        public static string GetSimpleName(this SyntaxNode node) =>
            node switch
            {
                CSSyntax.IdentifierNameSyntax csIdentifier => csIdentifier.Identifier.ValueText,
                VBSyntax.IdentifierNameSyntax vbIdentifier => vbIdentifier.Identifier.ValueText,
                _ => throw new ArgumentException("Syntax node must be an IdentifierNameSyntax to get its simple name", nameof(node))
            };

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

            // If the node is part of a qualified name or a member access expression, we want to get the full qualified name
            // which will be the parent of that node. It's not necessary to recurse to the parent's ancestors since qualified
            // names and member access expressions are composed of simple names on the right and the entire qualifier on the left.
            if ((node.Parent is CSSyntax.QualifiedNameSyntax csName && csName.Right.IsEquivalentTo(node))
                || (node.Parent is VBSyntax.QualifiedNameSyntax vbName && vbName.Right.IsEquivalentTo(node))
                || (node.Parent is CSSyntax.MemberAccessExpressionSyntax csMAE && csMAE.Name.IsEquivalentTo(node))
                || (node.Parent is VBSyntax.MemberAccessExpressionSyntax vbMAE && vbMAE.Name.IsEquivalentTo(node)))
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
            where T : SyntaxNode
        {
            var methodDeclNodes = node?.DescendantNodes().OfType<T>();

            return methodDeclNodes.FirstOrDefault(m =>
                    (m is CSSyntax.MethodDeclarationSyntax csM &&
                    csM.Identifier.ToString().Equals(methodName, StringComparison.Ordinal) &&
                    requiredParameterTypes.All(req => csM.ParameterList.Parameters.Any(p => string.Equals(p.Type?.ToString(), req, StringComparison.Ordinal))))
                || (m is VBSyntax.MethodStatementSyntax vbM &&
                    vbM.Identifier.ToString().Equals(methodName, StringComparison.Ordinal) &&
                    requiredParameterTypes.All(req => vbM.ParameterList.Parameters.Any(p => string.Equals(p.AsClause?.Type?.ToString(), req, StringComparison.Ordinal)))));
        }

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
        /// Determines if a syntax node is in a scope that includes a using or import statement for a given namespace.
        /// </summary>
        /// <param name="node">The node to analyze for access to the namespace in its scope.</param>
        /// <param name="namespaceName">The namespace name to check for.</param>
        /// <returns>True if the node is in a syntax tree with the given namespace in scope. False otherwise.</returns>
        public static bool HasAccessToNamespace(this SyntaxNode node, string namespaceName)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return node.AncestorsAndSelf().Any(n => IncludesImport(n, namespaceName));

            /// <summary>
            /// Determines if a syntax node includes a using or import statement for a given namespace.
            /// Will not return true if the node's children include the specified using/import but the node itself does not.
            /// </summary>
            /// <param name="node">The node to analyze.</param>
            /// <param name="namespaceName">The namespace name to check for.</param>
            /// <returns>True if the node has a direct import or using statement for the given namespace. False otherwise.</returns>
            static bool IncludesImport(SyntaxNode node, string namespaceName)
            {
                if (node is null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                if (node is CSSyntax.CompilationUnitSyntax || node is VBSyntax.CompilationUnitSyntax)
                {
                    return node.RootIncludesImport(namespaceName);
                }

                // Descend only into VB import statements
                var nodes = node.DescendantNodesAndSelf(n => VisualBasicExtensions.IsKind(n, VB.SyntaxKind.ImportsStatement), false);
                var children = node.ChildNodes();

                var usings = children.OfType<CSSyntax.UsingDirectiveSyntax>().Select(u => u.Name.ToString())
                    .Concat(children.OfType<VBSyntax.SimpleImportsClauseSyntax>().Select(i => i.Name.ToString()))
                    .Concat(children.OfType<VBSyntax.ImportsStatementSyntax>().SelectMany(i => i.ImportsClauses.OfType<VBSyntax.SimpleImportsClauseSyntax>().Select(i => i.Name.ToString())));

                return usings.Any(n => n.Equals(namespaceName, node.GetStringComparison()));
            }
        }

        public static CSSyntax.CompilationUnitSyntax AddImportIfMissing(this CSSyntax.CompilationUnitSyntax documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException($"'{nameof(namespaceName)}' cannot be null or empty.", nameof(namespaceName));
            }

            if (documentRoot.RootIncludesImport(namespaceName))
            {
                return documentRoot;
            }

            var usingDirective = CS.SyntaxFactory.UsingDirective(CS.SyntaxFactory.ParseName(namespaceName).WithLeadingTrivia(CS.SyntaxFactory.Whitespace(" ")))
                .WithTrailingTrivia(CS.SyntaxFactory.CarriageReturnLineFeed);
            return documentRoot.AddUsings(usingDirective);
        }

        public static VBSyntax.CompilationUnitSyntax AddImportIfMissing(this VBSyntax.CompilationUnitSyntax documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException($"'{nameof(namespaceName)}' cannot be null or empty.", nameof(namespaceName));
            }

            if (documentRoot.RootIncludesImport(namespaceName))
            {
                return documentRoot;
            }

            var importsStatement = VB.SyntaxFactory.ImportsStatement(VB.SyntaxFactory.SingletonSeparatedList<VBSyntax.ImportsClauseSyntax>(VB.SyntaxFactory.SimpleImportsClause(VB.SyntaxFactory.ParseName(namespaceName).WithLeadingTrivia(VB.SyntaxFactory.Whitespace(" ")))))
                .WithTrailingTrivia(VB.SyntaxFactory.CarriageReturnLineFeed);
            return documentRoot.AddImports(importsStatement);
        }

        /// <summary>
        /// Determines if a document root element contains a using/import statement for a given namespace.
        /// </summary>
        /// <param name="documentRoot">The document root to analyze.</param>
        /// <param name="namespaceName">The namespace name to look for an import for.</param>
        /// <returns>True if the documentRoot is a root node and contains a top-level using/import statement for the specified namespace name.</returns>
        private static bool RootIncludesImport(this SyntaxNode documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            return documentRoot switch
            {
                CSSyntax.CompilationUnitSyntax csRoot =>
                    csRoot.Usings.Any(u => u.Name.ToString().Equals(namespaceName, StringComparison.Ordinal)),
                VBSyntax.CompilationUnitSyntax vbRoot =>
                    vbRoot.Imports.SelectMany(i => i.ImportsClauses.OfType<VBSyntax.SimpleImportsClauseSyntax>()).Any(i => i.Name.ToString().Equals(namespaceName, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        public static SyntaxNode? GetInvocationExpression(this SyntaxNode callerNode)
        {
            if (callerNode.IsVisualBasic())
            {
                return callerNode.FirstAncestorOrSelf<VBSyntax.InvocationExpressionSyntax>();
            }
            else if (callerNode.IsCSharp())
            {
                return callerNode.FirstAncestorOrSelf<CSSyntax.InvocationExpressionSyntax>();
            }

            throw new NotImplementedException(Resources.UnknownLanguage);
        }

        /// <summary>
        /// Gets the ExpressionSyntax (the left part of the member access) from a C# or VB MemberAccessExpressionSyntax.
        /// </summary>
        /// <param name="memberAccessSyntax">The syntax whose expression propety should be returned. Note that this syntax must be of type Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax or Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax.</param>
        /// <returns>The member access expression's expression (the left part of the member access).</returns>
        public static SyntaxNode? GetChildExpressionSyntax(this SyntaxNode memberAccessSyntax) =>
            memberAccessSyntax switch
            {
                CSSyntax.MemberAccessExpressionSyntax csMAE => csMAE.Expression,
                VBSyntax.MemberAccessExpressionSyntax vbMAE => vbMAE.Expression,
                _ => null
            };

        public static StringComparison GetStringComparison(this SyntaxNode? node)
            => node?.Language switch
            {
                LanguageNames.CSharp => StringComparison.Ordinal,
                LanguageNames.VisualBasic => StringComparison.OrdinalIgnoreCase,
                _ => throw new NotImplementedException(Resources.UnknownLanguage),
            };

        public static StringComparer GetStringComparer(this SyntaxNode? node)
            => node?.Language switch
            {
                LanguageNames.CSharp => StringComparer.Ordinal,
                LanguageNames.VisualBasic => StringComparer.OrdinalIgnoreCase,
                _ => throw new NotImplementedException(Resources.UnknownLanguage),
            };

        /// <summary>
        /// Attempts to determine if a given syntax node corresponds to a type symbol
        /// based on the node's usage by its parent. This is a best guess in cases where
        /// semantic information is not available.
        /// </summary>
        /// <remarks>
        /// Checking the symbol in the semantic model is preferred and this method
        /// should only be used when no symbol information is available.
        /// </remarks>
        /// <param name="node">The syntax node to investigate.</param>
        /// <returns>True if it appears the syntax node is used in a context where a type
        /// symbol would usually make sense.</returns>
        public static bool IsTypeSyntax(this SyntaxNode? node)
        {
            var parentNode = node?.Parent;

            if (parentNode is null)
            {
                return false;
            }

            // Object creation
            if (parentNode.IsKind(CS.SyntaxKind.ObjectCreationExpression) || parentNode.IsKind(VB.SyntaxKind.ObjectCreationExpression))
            {
                return true;
            }

            // Static member access
            if ((parentNode is CSSyntax.MemberAccessExpressionSyntax csMAE && csMAE.Expression.IsEquivalentTo(node))
                || (parentNode is VBSyntax.MemberAccessExpressionSyntax vbMAE && vbMAE.Expression.IsEquivalentTo(node)))
            {
                return true;
            }

            // Base type
            if (parentNode.IsBaseTypeSyntax())
            {
                return true;
            }

            // Generic parameter
            if (parentNode.IsKind(CS.SyntaxKind.TypeArgumentList) || parentNode.IsKind(VB.SyntaxKind.TypeArgumentList))
            {
                return true;
            }

            // Attribute
            if (parentNode.IsKind(CS.SyntaxKind.Attribute) || parentNode.IsKind(VB.SyntaxKind.Attribute))
            {
                return true;
            }

            // Array
            if (parentNode.IsKind(CS.SyntaxKind.ArrayType) || parentNode.IsKind(VB.SyntaxKind.ArrayType))
            {
                return true;
            }

            // Variable type
            if ((parentNode is CSSyntax.VariableDeclarationSyntax csDecl && csDecl.Type.IsEquivalentTo(node))
                || (parentNode is VBSyntax.SimpleAsClauseSyntax vbDecl && vbDecl.Type.IsEquivalentTo(node)))
            {
                return true;
            }

            // Parameter type
            if (parentNode.IsKind(CS.SyntaxKind.Parameter) || parentNode.IsKind(VB.SyntaxKind.Parameter))
            {
                return true;
            }

            // Property type
            if (parentNode.IsKind(CS.SyntaxKind.PropertyDeclaration) || parentNode.IsKind(VB.SyntaxKind.PropertyStatement))
            {
                return true;
            }

            // Method return type (VB method return types are already covered by SimpleAsClause
            if (parentNode.IsKind(CS.SyntaxKind.MethodDeclaration))
            {
                return true;
            }

            // C# As expression
            if (parentNode is CSSyntax.BinaryExpressionSyntax binaryExpression
                && binaryExpression.IsKind(CS.SyntaxKind.AsExpression)
                && binaryExpression.Right.IsEquivalentTo(node))
            {
                return true;
            }

            // VB CType expression
            if (parentNode is VBSyntax.CTypeExpressionSyntax cType
                && cType.Type.IsEquivalentTo(node))
            {
                return true;
            }

            // C# cast expression
            if (parentNode is CSSyntax.CastExpressionSyntax csCastExpression
                && csCastExpression.Type.IsEquivalentTo(node))
            {
                return true;
            }

            // VB cast expression
            if (parentNode is VBSyntax.CastExpressionSyntax vbCastExpression
                && vbCastExpression.Type.IsEquivalentTo(node))
            {
                return true;
            }

            return false;
        }
    }
}
