using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    public abstract class IdentifierMigrationCodeFixer : CodeFixProvider
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

            var node = root.FindNode(context.Span);

            if (node is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            if (diagnostic is null)
            {
                return;
            }

            if (diagnostic.Properties.TryGetValue(IdentifierMigrationAnalyzer.NewIdentifierKey, out var property) && property is not null)
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
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var name = node switch
            {
                // The most common case is that the node is an IdentifierName
                NameSyntax n => n,

                // In some cases (when the node is a SimpleBaseType, for example) we need to get the name from a child node
                SyntaxNode x => x.DescendantNodesAndSelf().FirstOrDefault(n => n is NameSyntax)
            };

            // If we can't find a name, bail out
            if (name is null)
            {
                return document;
            }

            // Update to new identifier
            SyntaxNode updatedName = SyntaxFactory.ParseName(newIdentifier)
                .WithTriviaFrom(name)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            // Make sure the name syntax node includes the whole name in case it is qualified
            if (name.Parent is NameSyntax)
            {
                while (name.Parent is NameSyntax)
                {
                    name = name.Parent;

                    if (name is null)
                    {
                        return document;
                    }
                }
            }

            // In some cases (accessing a static member), the name may be part of a member access expression chain
            // instead of a qualified name. If that's the case, update the name and updatedName accordingly.
            else if (name.Parent is MemberAccessExpressionSyntax m && m.Name.ToString().Equals(name.ToString(), StringComparison.Ordinal))
            {
                name = name.Parent;

                if (name is null)
                {
                    return document;
                }

                updatedName = SyntaxFactory.ParseExpression(newIdentifier)
                    .WithTriviaFrom(name)
                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            // Work on the document root instead of updating nodes with the editor directly so that
            // additional changes can be made later (adding using statement) if necessary.
            if (editor.OriginalRoot is not CompilationUnitSyntax documentRoot)
            {
                return document;
            }

            documentRoot = documentRoot.ReplaceNode(name, updatedName)!;

            // Add using declation if needed
            var namespaceName = GetNamespace(newIdentifier);
            documentRoot = documentRoot.AddUsingIfMissing(namespaceName);

            editor.ReplaceNode(editor.OriginalRoot, documentRoot);

            return editor.GetChangedDocument();
        }

        private static string GetNamespace(string newIdentifier) => newIdentifier.Substring(0, newIdentifier.LastIndexOf('.'));
    }
}
