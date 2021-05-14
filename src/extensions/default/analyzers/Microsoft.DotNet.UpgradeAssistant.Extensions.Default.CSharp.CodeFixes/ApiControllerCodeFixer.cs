// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "UA0013 CodeFix Provider")]
    public class ApiControllerCodeFixer : CodeFixProvider
    {
        public const string GoodNamespace = "Microsoft.AspNetCore.Mvc";
        public const string GoodClassName = "Controller";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ApiControllerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var baseTypeNode = ApiControllerAnalyzer.GetBaseTypeFromSyntax(root.FindNode(context.Span));

            if (baseTypeNode is null)
            {
                // should never happen, but prevents an exception if there is a scenario I didn't remember
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.ApiControllerTitle,
                    cancellationToken => ReplaceBaseClass(context.Document, baseTypeNode, cancellationToken),
                    nameof(CodeFixResources.ApiControllerTitle)),
                context.Diagnostics);
        }

        /// <summary>
        /// Built with the assumption that the <paramref name="node"/> is a IdentifierNameSyntax or QualifiedNameSyntax that indicates an ApiController BaseType.
        /// </summary>
        /// <param name="document">The document that contains a class to fix.</param>
        /// <param name="node">A class that is ApiController.</param>
        /// <param name="cancellationToken">May request cancellation.</param>
        /// <returns>A document where at least one ApiController was removed.</returns>
        private static async Task<Document> ReplaceBaseClass(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var mvcBaseType = generator.QualifiedName(
                    QualifiedNameBuilder.BuildQualifiedSyntax(generator, GoodNamespace),
                    generator.IdentifierName(GoodClassName))
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

            var newRoot = generator.ReplaceNode(root, node, mvcBaseType);

            var newDocument = document.WithSyntaxRoot(newRoot);

            newDocument = await ImportAdder.AddImportsAsync(newDocument, Simplifier.AddImportsAnnotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            return newDocument;
        }
    }
}
