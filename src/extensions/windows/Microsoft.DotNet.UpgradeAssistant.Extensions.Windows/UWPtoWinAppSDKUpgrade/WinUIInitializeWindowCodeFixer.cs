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
    public class WinUIInitializeWindowCodeFixer : CodeFixProvider
    {
        private const string WindowInitializerComment = @"/* TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
            Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd */
        ";

        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        private const string DiagnosticId = WinUIInitializeWindowAnalyzer.DiagnosticId;

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

            var declarations = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    DiagnosticId,
                    c => FixContentDialogAPI(context.Document, declarations ?? ImmutableList.Create<ObjectCreationExpressionSyntax>(), c),
                    equivalenceKey: DiagnosticId),
                context.Diagnostics);
        }

        private static async Task<Document> FixContentDialogAPI(Document document, IEnumerable<ObjectCreationExpressionSyntax> objectCreationDeclarations, CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            foreach (var objectCreationDeclaration in objectCreationDeclarations)
            {
                var newMethodDeclarationSibling = objectCreationDeclaration.Ancestors().OfType<MethodDeclarationSyntax>().First();
                var typeName = objectCreationDeclaration.Type.ToString();

                var newMethodCall = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("InitializeWithWindow"),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(objectCreationDeclaration),
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression("App.WindowHandle"))
                    })));

                var newMethodCallComment = SyntaxFactory.Comment(WindowInitializerComment);
                documentEditor.ReplaceNode(objectCreationDeclaration, newMethodCall.WithLeadingTrivia(newMethodCallComment));

                if (!newMethodDeclarationSibling.Parent!.ChildNodes().OfType<MethodDeclarationSyntax>()
                    .Any(sibling => sibling.Identifier.ValueText == "InitializeWithWindow"
                        && sibling.ParameterList.Parameters.Any(parameter => parameter.Type!.ToString() == typeName)))
                {
                    var newMethodRoot = await CSharpSyntaxTree.ParseText(@$"
                    class A
                    {{
                        private static {typeName} InitializeWithWindow({typeName} obj, IntPtr windowHandle)
                        {{
                            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
                            return obj;
                        }}
                    }}",
                    cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var newMethodDeclaration = newMethodRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
                    documentEditor.InsertAfter(newMethodDeclarationSibling, newMethodDeclaration);
                }
            }

            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
