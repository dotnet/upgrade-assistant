// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIInteropCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        private const string DiagnosticId = WinUIInteropAnalyzer.DiagnosticId;

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null || !context.Diagnostics.Any())
            {
                return;
            }

            var node = root.FindNode(context.Span, findInsideTrivia: false, getInnermostNodeForTie: true);

            var diagnostic = context.Diagnostics.First();
            var apiId = diagnostic.Properties["apiId"]!;
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First()!;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    DiagnosticId,
                    c => FixInteropAPI(context.Document, root, declaration, apiId, c),
                    DiagnosticId + apiId),
                context.Diagnostics);
        }

        private static async Task<Document> FixInteropAPI(Document document, SyntaxNode root, InvocationExpressionSyntax invocationExpressionSyntax, string apiId, CancellationToken cancellationToken)
        {
            var mappedApi = WinUIInteropAnalyzer.UWPToWinUIInteropAPIMap[apiId]!.Value;
            if (!mappedApi.HasValue)
            {
                var comment = await CSharpSyntaxTree.ParseText(@$"
                /*
                   TODO: This api is not supported in Windows App SDK yet.
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
                */
                ", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root!.ReplaceNode<SyntaxNode>(invocationExpressionSyntax, invocationExpressionSyntax.WithLeadingTrivia(comment.GetLeadingTrivia()));
                return document.WithSyntaxRoot(newRoot);
            }

            var (newTypeNamespace, newTypeName, newMethodName) = mappedApi!.Value;

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var argsToAdd = invocationExpressionSyntax.DescendantNodes().OfType<ArgumentListSyntax>().First().Arguments.ToArray();

            var newExpressionRoot = await CSharpSyntaxTree.ParseText(@$"
                {newTypeNamespace}.{newTypeName}.{newMethodName}(App.WindowHandle)
                ", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);

            var newExpression = newExpressionRoot.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            var newExpressionArgs = newExpression.DescendantNodes().OfType<ArgumentListSyntax>().First();
            var newArgs = newExpressionArgs.AddArguments(argsToAdd);
            var newExpressionWithArgs = newExpression.ReplaceNode(newExpressionArgs, newArgs);

            documentEditor.ReplaceNode(invocationExpressionSyntax, newExpressionWithArgs);
            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
