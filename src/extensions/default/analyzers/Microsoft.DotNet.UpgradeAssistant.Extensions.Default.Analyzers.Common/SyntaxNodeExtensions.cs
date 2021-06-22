// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
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

            // If the node is part of a qualified name, we want to get the full qualified name
            while (node.Parent is CSSyntax.NameSyntax || node.Parent is VBSyntax.NameSyntax)
            {
                node = node.Parent;
            }

            // If the node is part of a member access expression (a static member access, for example), then the
            // qualified name will be a member access expression rather than a name syntax.
            if ((node.Parent is CSSyntax.MemberAccessExpressionSyntax csMAE && csMAE.Name.IsEquivalentTo(node))
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

            return node.AncestorsAndSelf().Any(n => n.IncludesImport(namespaceName));
        }

        /// <summary>
        /// Determines if a syntax node includes a using or import statement for a given namespace.
        /// Will not return true if the node's children include the specified using/import but the node itself does not.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="namespaceName">The namespace name to check for.</param>
        /// <returns>True if the node has a direct import or using statement for the given namespace. False otherwise.</returns>
        private static bool IncludesImport(this SyntaxNode node, string namespaceName)
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

            return usings.Any(n => n.Equals(namespaceName, node.Language == LanguageNames.CSharp ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
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

        /// <summary>
        /// Adds a using or import directive for a given namespace to the document root only if the directive is not already present.
        /// </summary>
        /// <param name="document">The document to update.</param>
        /// <param name="namespaceName">The namespace to add.</param>
        /// <param name="node">The syntax node that needs access to the namespace.
        /// If this is null, the using/import will be added if there isn't a global import already present for the namespace.
        /// If this is not null, the import will only be added if the namespace is not available to the given syntax node
        /// (so imports within a namespace can be used).</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>An updated document with the necessary import added if it was not present previously.</returns>
        public static async Task<Document> AddImportIfMissingAsync(this Document document, string namespaceName, SyntaxNode? node, CancellationToken ct)
        {
            // This should be possible using the ImportAdder, but the ImportAdder does not support scenarios with compiler errors well, especially in the case of Visual Basic.
            // TODO: Investigate the feasibility of using ImportAdder in the future. For now, this method implements its own simple import addition logic.
            /*
                return node is null
                    ? await ImportAdder.AddImportsAsync(document, CodeAnalysis.Simplification.Simplifier.AddImportsAnnotation, null, ct).ConfigureAwait(false)
                    : await ImportAdder.AddImportsAsync(document, node.FullSpan, null, ct).ConfigureAwait(false);
            */

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException($"'{nameof(namespaceName)}' cannot be null or empty.", nameof(namespaceName));
            }

            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null)
            {
                return document;
            }

            var updatedRoot = root.AddImportIfMissing(namespaceName, SyntaxGenerator.GetGenerator(document), node);

            return document.WithSyntaxRoot(updatedRoot);
        }

        /// <summary>
        /// Adds a using or import directive for a given namespace to the document root only if the directive is not already present.
        /// </summary>
        /// <param name="documentRoot">The document root to be updated. This should be a C# or VB CompilationUnitSyntax (document root syntax node).</param>
        /// <param name="namespaceName">The namespace to add.</param>
        /// <param name="generator">The syntax generator to use to create new import statements.</param>
        /// <param name="node">The syntax node that needs access to the namespace.
        /// If this is null, the using/import will be added if there isn't a global import already present for the namespace.
        /// If this is not null, the import will only be added if the namespace is not available to the given syntax node
        /// (so imports within a namespace can be used).</param>
        /// <returns>An updated document root with the necessary import added if it was not present previously.</returns>
        public static SyntaxNode AddImportIfMissing(this SyntaxNode documentRoot, string namespaceName, SyntaxGenerator generator, SyntaxNode? node)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException($"'{nameof(namespaceName)}' cannot be null or empty.", nameof(namespaceName));
            }

            if (generator is null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            // If a target node is provided, bail out if that node has access to the given namespace
            if (node is not null)
            {
                if (node.HasAccessToNamespace(namespaceName))
                {
                    return documentRoot;
                }
            }

            // If no target node is provided, bail out only if there is a global import present for the given namespace
            else
            {
                if (documentRoot.RootIncludesImport(namespaceName))
                {
                    return documentRoot;
                }
            }

            // Create the import statement
            var importDeclaration = generator.NamespaceImportDeclaration(namespaceName);

            // Add the import to the document root
            if (documentRoot is CSSyntax.CompilationUnitSyntax csRoot)
            {
                documentRoot = csRoot.AddUsings((CSSyntax.UsingDirectiveSyntax)importDeclaration);
            }
            else if (documentRoot is VBSyntax.CompilationUnitSyntax vbRoot)
            {
                documentRoot = vbRoot.AddImports((VBSyntax.ImportsStatementSyntax)importDeclaration);
            }

            // Return the document with an updated root
            return documentRoot;
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
    }
}
