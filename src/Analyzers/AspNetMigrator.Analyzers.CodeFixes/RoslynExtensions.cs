using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AspNetMigrator.Analyzers
{
    public static class RoslynExtensions
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
            var result = anyUsings ? documentRoot : documentRoot.AddUsings(UsingDirective(ParseName(namespaceName)));

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
                    m.Identifier.ToString().Equals(methodName) &&
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
    }
}
