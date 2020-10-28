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
    public abstract class IdentiferMigrationCodeFixer : CodeFixProvider
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

        private async Task<Document> UpdateResultTypeAsync(Document document, SyntaxNode node, string newIdentifier, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (!(node is NameSyntax name))
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
            if (!documentRoot.Usings.Any(u => u.Name.ToString().Equals(namespaceName, StringComparison.Ordinal)))
            {
                documentRoot = documentRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
            }

            editor.ReplaceNode(editor.OriginalRoot, documentRoot);

            return editor.GetChangedDocument();
        }

        private string GetNamespace(string newIdentifier) => newIdentifier.Substring(0, newIdentifier.LastIndexOf('.'));
    }
}
