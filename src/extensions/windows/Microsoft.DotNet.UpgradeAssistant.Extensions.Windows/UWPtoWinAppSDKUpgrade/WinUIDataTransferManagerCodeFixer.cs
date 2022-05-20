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
    public class WinUIDataTransferManagerCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        private const string DiagnosticId = WinUIDataTransferManagerAnalyzer.DiagnosticId;

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
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    DiagnosticId,
                    c => FixDataTransferManagerAPI(context.Document, declaration, c),
                    DiagnosticId),
                context.Diagnostics);
        }

        private static async Task<Document> FixDataTransferManagerAPI(Document document, InvocationExpressionSyntax? invocationExpressionSyntax, CancellationToken cancellationToken)
        {
            if (invocationExpressionSyntax is null)
            {
                return document;
            }

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var newExpressionRoot = await CSharpSyntaxTree.ParseText(@"
                Windows.ApplicationModel.DataTransfer.DataTransferManager.As<UWPToWinAppSDKUpgradeHelpers.IDataTransferManagerInterop>().ShowShareUIForWindow(App.WindowHandle)
                ", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newExpression = newExpressionRoot.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            documentEditor.ReplaceNode(invocationExpressionSyntax, newExpression);
            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
