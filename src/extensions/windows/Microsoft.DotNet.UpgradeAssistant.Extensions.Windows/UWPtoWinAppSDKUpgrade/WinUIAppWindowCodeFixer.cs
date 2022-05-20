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
    public class WinUIAppWindowCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowNamespace,
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType,
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember);

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
            diagnostic.Properties.TryGetValue("apiId", out var apiId);
            diagnostic.Properties.TryGetValue("varName", out var varName);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindNode(diagnosticSpan)!;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    diagnostic.Id,
                    c => diagnostic.Id == WinUIAppWindowAnalyzer.DiagnosticIdAppWindowNamespace ? FixAppWindowNamespace(context.Document, root, declaration, apiId!, c)
                    : diagnostic.Id == WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType ? FixAppWindowVarType(context.Document, declaration, c)
                    : FixAppWindowMember(context.Document, declaration.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First(), apiId!, varName!, c),
                    diagnostic.Id + apiId ?? string.Empty),
                context.Diagnostics);
        }

        private static async Task<Document> FixAppWindowNamespace(Document document, SyntaxNode oldRoot, SyntaxNode invocationExpressionSyntax, string apiId, CancellationToken cancellationToken)
        {
            var comment = await CSharpSyntaxTree.ParseText(@$"
                /*
                   TODO: Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);

            var (newNamespace, newName) = WinUIAppWindowAnalyzer.TypeConversions[apiId].Value;
            var node = (await CSharpSyntaxTree.ParseText($"{newNamespace}.{newName} x;", cancellationToken: cancellationToken)
                .GetRootAsync(cancellationToken).ConfigureAwait(false))
                .DescendantNodesAndSelf().OfType<QualifiedNameSyntax>().First();
            return document.WithSyntaxRoot(oldRoot.ReplaceNode(invocationExpressionSyntax, node.WithLeadingTrivia(comment.GetLeadingTrivia())));
        }

        private static async Task<Document> FixAppWindowVarType(Document document, SyntaxNode invocationExpressionSyntax, CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            VariableDeclarationSyntax variableDeclarator = invocationExpressionSyntax.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            if (variableDeclarator is not null)
            {
                var varIdentifier = variableDeclarator.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.IsVar).FirstOrDefault();
                if (varIdentifier is not null)
                {
                    var node = (await CSharpSyntaxTree.ParseText("Microsoft.UI.Windowing.AppWindow x;", cancellationToken: cancellationToken)
                        .GetRootAsync(cancellationToken).ConfigureAwait(false))
                        .DescendantNodesAndSelf().OfType<QualifiedNameSyntax>().First();
                    documentEditor.ReplaceNode(varIdentifier, node);
                    return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
                }
            }

            return document;
        }

        private static async Task<Document> FixAppWindowMember(Document document, InvocationExpressionSyntax invocationExpressionSyntax, string apiId, string varName, CancellationToken cancellationToken)
        {
            var mappedApi = WinUIAppWindowAnalyzer.MemberConversions[apiId]!.Value;
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var (newTypeNamespace, newTypeName, newMethodName) = mappedApi!.Value;

            var argsToAdd = invocationExpressionSyntax.DescendantNodes().OfType<ArgumentListSyntax>().First().Arguments.ToArray();

            var instance = newTypeNamespace == "*" ? varName : $"{newTypeNamespace}.{newTypeName}";
            var newExpressionRoot = await CSharpSyntaxTree.ParseText(@$"
                {instance}.{newMethodName}()
                ", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);

            var newExpression = newExpressionRoot.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            var newExpressionArgs = newExpression.DescendantNodes().OfType<ArgumentListSyntax>().First();
            var newArgs = newExpressionArgs.AddArguments(argsToAdd);
            var newExpressionWithArgs = newExpression.ReplaceNode(newExpressionArgs, newArgs);

            if (invocationExpressionSyntax.Expression.ToString().EndsWith("Async", StringComparison.Ordinal)
                && !newMethodName.EndsWith("Async", StringComparison.Ordinal))
            {
                var awaitExpression = invocationExpressionSyntax.Ancestors().OfType<AwaitExpressionSyntax>().FirstOrDefault();
                if (awaitExpression is not null)
                {
                    documentEditor.ReplaceNode(awaitExpression, newExpressionWithArgs);
                    return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
                }
            }

            documentEditor.ReplaceNode(invocationExpressionSyntax, newExpressionWithArgs);
            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
