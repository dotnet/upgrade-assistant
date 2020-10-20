using System;
using System.Collections.Immutable;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AM002 CodeFix Provider")]
    class HtmlStringCodeFixer : CodeFixProvider
    {
        const string AspNetCoreHtmlNamespaceName = "Microsoft.AspNetCore.Html";
        const string HtmlStringIdentifier = "HtmlString";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HtmlStringAnalyzer.DiagnosticId);

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

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.HtmlStringTitle,
                    cancellationToken => ReplaceWithHtmlStringAsync(context.Document, node, cancellationToken),
                    nameof(CodeFixResources.HtmlStringTitle)),
                context.Diagnostics);
        }

        private async Task<Document> ReplaceWithHtmlStringAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
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

            // Update to HtmlString
            var updatedName = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(AspNetCoreHtmlNamespaceName), SyntaxFactory.IdentifierName(HtmlStringIdentifier))
                .WithTriviaFrom(name)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            // Work on the document root instead of updating nodes with the editor directly so that
            // additional changes can be made later (adding using statement) if necessary.
            var documentRoot = editor.OriginalRoot as CompilationUnitSyntax;
            documentRoot = documentRoot.ReplaceNode(name, updatedName);

            // Add using declation if needed
            if (!documentRoot.Usings.Any(u => u.Name.ToString().Equals(AspNetCoreHtmlNamespaceName, StringComparison.Ordinal)))
            {
                documentRoot =  documentRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(AspNetCoreHtmlNamespaceName)));
            }

            editor.ReplaceNode(editor.OriginalRoot, documentRoot);

            return editor.GetChangedDocument();
        }
    }
}
