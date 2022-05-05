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
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIBackButtonCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WinUIBackButtonAnalyzer.DiagnosticId);

        private const string BackButtonComment = @$"
            /*
              {BackButtonMessage}
            */";

        private const string BackButtonMessage = @$"
            TODO {WinUIBackButtonAnalyzer.DiagnosticId} Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button ""{WinUIBackButtonXamlUpdater.NewBackButtonName}"" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015";

        private ILogger<WinUIBackButtonCodeFixer>? _logger;

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public WinUIBackButtonCodeFixer(ILogger<WinUIBackButtonCodeFixer>? logger)
        {
            this._logger = logger;
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
            var lineSpan = diagnostic.Location.GetLineSpan();
            var fixState = diagnostic.Properties[WinUIBackButtonAnalyzer.FixStateProperty]!;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AssignmentExpressionSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    WinUIBackButtonAnalyzer.DiagnosticId,
                    c => FixBackButton(context.Document, declaration, fixState, lineSpan, c),
                    equivalenceKey: WinUIBackButtonAnalyzer.DiagnosticId + fixState),
                diagnostic);
        }

        private async Task<Document> FixBackButton(Document document, AssignmentExpressionSyntax backButtonAssignment, string fixState, FileLinePositionSpan lineSpan, CancellationToken cancellationToken)
        {
            _logger?.LogWarning(lineSpan.Path + BackButtonMessage, null);
            var comment = await CSharpSyntaxTree.ParseText(BackButtonComment, cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
            StatementSyntax newBackButtonAssignmentWithComment;

            if (backButtonAssignment.Left.ToString().Contains("AppViewBackButtonVisibility")
                && (backButtonAssignment.Right.ToString() == "AppViewBackButtonVisibility.Collapsed"
                || backButtonAssignment.Right.ToString() == "AppViewBackButtonVisibility.Visible"))
            {
                var backButtonVisibility = backButtonAssignment.Right.ToString().Contains("Collapsed") ? "Collapsed" : "Visible";
                var newBackButtonAssignmentTree = await CSharpSyntaxTree.ParseText(
                    $"{WinUIBackButtonXamlUpdater.NewBackButtonName}.Visibility = Microsoft.UI.Xaml.Visibility.{backButtonVisibility};", cancellationToken: cancellationToken)
                    .GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newBackButtonAssignment = ((GlobalStatementSyntax)newBackButtonAssignmentTree.ChildNodesAndTokens()[0].AsNode()!).Statement;
                newBackButtonAssignmentWithComment = newBackButtonAssignment.WithLeadingTrivia(comment.GetLeadingTrivia());
            }
            else
            {
                newBackButtonAssignmentWithComment = backButtonAssignment.Ancestors().OfType<StatementSyntax>().First().WithLeadingTrivia(comment.GetLeadingTrivia());
            }

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            documentEditor.ReplaceNode(backButtonAssignment.Parent!, newBackButtonAssignmentWithComment);

            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
