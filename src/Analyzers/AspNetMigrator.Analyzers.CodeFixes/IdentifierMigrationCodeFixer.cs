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

namespace AspNetMigrator.Analyzers
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
            var node = root.FindNode(context.Span);

            if (node == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixTitle,
                    cancellationToken => UpdateResultTypeAsync(context.Document, node, diagnostic.Properties[IdentifierMigrationAnalyzer.NewIdentifierKey], cancellationToken),
                    CodeFixTitle),
                context.Diagnostics);
        }

        private static async Task<Document> UpdateResultTypeAsync(Document document, SyntaxNode node, string newIdentifier, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var name = node switch
            {
                // The most common case is that the node is an IdentifierName
                NameSyntax n => n,

                // In some cases (when the node is a SimpleBaseType, for example) we need to get the name from a child node
                SyntaxNode x => x.DescendantNodesAndSelf().FirstOrDefault(n => n is NameSyntax) as NameSyntax
            };

            // If we can't find a name, bail out
            if (name is null)
            {
                return document;
            }

            // Make sure the name syntax node includes the whole name in case it is qualified
            while (name.Parent is NameSyntax)
            {
                name = name.Parent as NameSyntax;
            }

            // Update to new identifier
            var updatedName = SyntaxFactory.ParseName(newIdentifier)
                .WithTriviaFrom(name)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            // Work on the document root instead of updating nodes with the editor directly so that
            // additional changes can be made later (adding using statement) if necessary.
            var documentRoot = editor.OriginalRoot as CompilationUnitSyntax;
            documentRoot = documentRoot.ReplaceNode(name, updatedName);

            // Add using declation if needed
            var namespaceName = GetNamespace(newIdentifier);
            documentRoot = documentRoot.AddUsingIfMissing(namespaceName);

            editor.ReplaceNode(editor.OriginalRoot, documentRoot);

            return editor.GetChangedDocument();
        }

        private static string GetNamespace(string newIdentifier) => newIdentifier.Substring(0, newIdentifier.LastIndexOf('.'));
    }
}
