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
    internal class WinUIBackButtonCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WinUIBackButtonAnalyzer.DiagnosticId);

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

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AssignmentExpressionSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    "",
                    c => FixBackButton(context.Document, declaration, c),
                    "Fix Back Button"),
                diagnostic);
        }

        private static async Task<Document> FixBackButton(Document document, AssignmentExpressionSyntax backButtonAssignment, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var backMethodRoot = await CSharpSyntaxTree.ParseText(@"
            class A
            {
            private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            {
                Frame.GoBack();
            }
            }
            ").GetRootAsync(cancellationToken).ConfigureAwait(false);
            var backMethod = backMethodRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

            SyntaxNode backMethodSibling = backButtonAssignment;
            while (!backMethodSibling.IsKind(SyntaxKind.MethodDeclaration))
            {
                backMethodSibling = backMethodSibling!.Parent!;
            }

            var newBackButtonAssignmentTree = await CSharpSyntaxTree.ParseText("BackButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;").GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newBackButtonAssignment = ((GlobalStatementSyntax)newBackButtonAssignmentTree.ChildNodesAndTokens()[0].AsNode()!).Statement;

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            documentEditor.ReplaceNode(backButtonAssignment.Parent!, newBackButtonAssignment);
            if (!backMethodSibling.Parent!.ChildNodes().Any(sibling => sibling.GetText().ToString().Contains("BackButton_Click")))
            {
                documentEditor.InsertAfter(backMethodSibling, ImmutableArray.Create(backMethod));
            }

            return documentEditor.GetChangedDocument();
        }
    }
}
