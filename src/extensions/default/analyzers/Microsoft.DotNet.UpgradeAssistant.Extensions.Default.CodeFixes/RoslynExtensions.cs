// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    internal static class RoslynExtensions
    {
        /// <summary>
        /// Adds a using directive for a given namespace to the document root only if the directive is not already present.
        /// </summary>
        /// <param name="documentRoot">The document to add the directive to.</param>
        /// <param name="namespaceName">The namespace to reference with the using directive.</param>
        /// <returns>An updated document root with the specific using directive.</returns>
        public static CompilationUnitSyntax AddUsingIfMissing(this CompilationUnitSyntax documentRoot, string namespaceName)
        {
            if (documentRoot is null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            var anyUsings = documentRoot.Usings.Any(u => u.Name.ToString().Equals(namespaceName, StringComparison.Ordinal));
            var usingDirective = UsingDirective(ParseName(namespaceName).WithLeadingTrivia(Whitespace(" ")));
            var result = anyUsings ? documentRoot : documentRoot.AddUsings(usingDirective);

            return result;
        }

        /// <summary>
        /// Gets a method declared in a given syntax node.
        /// </summary>
        /// <param name="node">The syntax node to find the method declaration in.</param>
        /// <param name="methodName">The name of the method to return.</param>
        /// <param name="requiredParameterTypes">An optional list of parameter types that the method must accept.</param>
        /// <returns>The first method declaration in the syntax node with the given name and parameter types, or null if no such method declaration exists.</returns>
        public static MethodDeclarationSyntax? GetMethodDeclaration(this SyntaxNode node, string methodName, params string[] requiredParameterTypes) =>
            node?.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m =>
                    m.Identifier.ToString().Equals(methodName, StringComparison.Ordinal) &&
                    requiredParameterTypes.All(req => m.ParameterList.Parameters.Any(p => string.Equals(p.Type?.ToString(), req, StringComparison.Ordinal))));

        /// <summary>
        /// Applies whitespace trivia and new line trivia from another statement syntax to this one.
        /// </summary>
        /// <param name="statement">The StatementSyntax to update.</param>
        /// <param name="otherStatement">The StatementSyntax to copy trivia from.</param>
        /// <returns>The original statement syntax updated with the other statement syntax's whitespace and new line trivia.</returns>
        public static StatementSyntax WithWhitespaceTriviaFrom(this StatementSyntax statement, StatementSyntax otherStatement) =>
            statement
                .WithLeadingTrivia(otherStatement?.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.IsKind(SyntaxKind.WhitespaceTrivia)) ?? SyntaxTriviaList.Empty)
                .WithTrailingTrivia(otherStatement?.GetTrailingTrivia().Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.IsKind(SyntaxKind.WhitespaceTrivia)) ?? SyntaxTriviaList.Empty);

        /// <summary>
        /// Gets an existing parameter of the type from node if it is a method.
        /// </summary>
        /// <param name="node">A node that is potentially a method.</param>
        /// <param name="semanticModel">The current semantic model.</param>
        /// <param name="type">The type that should be matched.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A parameter symbol if a match is found.</returns>
        public static IParameterSymbol? GetExistingParameterSymbol(this SyntaxNode node, SemanticModel semanticModel, ITypeSymbol? type, CancellationToken token)
        {
            if (type is null)
            {
                return null;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node, token);

            if (symbol is not IMethodSymbol method)
            {
                return null;
            }

            return method.Parameters.FirstOrDefault(p =>
            {
                if (p.Type is null)
                {
                    return false;
                }

                return SymbolEqualityComparer.IncludeNullability.Equals(p.Type, type);
            });
        }

        /// <summary>
        /// Gets a parent operation that satisfies the given predicate.
        /// </summary>
        /// <param name="operation">A starting operation.</param>
        /// <param name="func">A predicate to check matches against.</param>
        /// <returns>A matching operation if found.</returns>
        public static IOperation? GetParentOperation(this IOperation? operation, Func<IOperation, bool> func)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            while (operation is not null)
            {
                if (func(operation))
                {
                    return operation;
                }

                operation = operation.Parent;
            }

            return default;
        }

        public static bool TryGetDocument(this Solution sln, SyntaxTree? tree, [MaybeNullWhen(false)] out Document document)
        {
            if (tree is null || sln is null)
            {
                document = null;
                return false;
            }

            foreach (var project in sln.Projects)
            {
                var doc = project.GetDocument(tree);

                if (doc is not null)
                {
                    document = doc;
                    return true;
                }
            }

            document = null;
            return false;
        }
    }
}
