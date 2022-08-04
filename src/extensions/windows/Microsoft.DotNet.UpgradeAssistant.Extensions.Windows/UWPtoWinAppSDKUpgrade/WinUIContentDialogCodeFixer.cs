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
    public class WinUIContentDialogCodeFixer : CodeFixProvider
    {
        private const string DialogSetterComment = "/* TODO You should replace 'this' with the instance of UserControl that is ContentDialog is meant to be a part of. */";

        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WinUIContentDialogAnalyzer.DiagnosticId);

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

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    WinUIContentDialogAnalyzer.DiagnosticId,
                    c => FixContentDialogAPI(context.Document, declaration, c),
                    WinUIContentDialogAnalyzer.DiagnosticId),
                diagnostic);
        }

        private static async Task<Document> FixContentDialogAPI(Document document, MemberAccessExpressionSyntax contentDialogMemberAccess, CancellationToken cancellationToken)
        {
            var newMethodDeclarationSibling = contentDialogMemberAccess.Ancestors().OfType<MethodDeclarationSyntax>().First();

            var newMethodCall = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("SetContentDialogRoot"),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(contentDialogMemberAccess.Expression),
                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression("this"))
                })));

            var newMethodCallComment = SyntaxFactory.Comment(DialogSetterComment);
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            documentEditor.ReplaceNode(contentDialogMemberAccess.Expression, newMethodCall.WithLeadingTrivia(newMethodCallComment));
            if (!newMethodDeclarationSibling.Parent!.ChildNodes().OfType<MethodDeclarationSyntax>()
                .Any(sibling => sibling.Identifier.ValueText == "SetContentDialogRoot"))
            {
                var newMethodRoot = await CSharpSyntaxTree.ParseText(@"
                class A
                {
                    private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog, UserControl control)
                    {
                        if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent(""Windows.Foundation.UniversalApiContract"", 8))
                        {
                            contentDialog.XamlRoot = control.Content.XamlRoot;
                        }
                        return contentDialog;
                    }
                }", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newMethodDeclaration = newMethodRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
                documentEditor.InsertAfter(newMethodDeclarationSibling, newMethodDeclaration);
            }

            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
