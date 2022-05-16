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
    public class WinUIMRTResourceManagerCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId,
            WinUIMRTResourceManagerAnalyzer.ResourceContextAPIDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
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

            var declaration = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    FixableDiagnosticIds.First(),
                    c => diagnostic.Id == WinUIMRTResourceManagerAnalyzer.ResourceManagerAPIDiagnosticId ? FixResourceManagerAPI(context.Document, declaration, c)
                        : FixResourceContextAPI(context.Document, declaration, c),
                    "MRT to MRT Core ResourceManager"),
                diagnostic);
        }

        private static async Task<Document> FixResourceManagerAPI(Document document, MemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newMemberInitiationRoot = await CSharpSyntaxTree.ParseText("new Microsoft.Windows.ApplicationModel.Resources.ResourceManager()", cancellationToken: cancellationToken)
                .GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newMemberInitiation = newMemberInitiationRoot.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            return document.WithSyntaxRoot(oldRoot!.ReplaceNode(memberAccessExpression, newMemberInitiation));
        }

        private static async Task<Document> FixResourceContextAPI(Document document, MemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocation = memberAccessExpression.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocation == null)
            {
                return document;
            }

            var comment = await CSharpSyntaxTree.ParseText(@"/*
                TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
                Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
                replace the new instance created below with correct instance.
                Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
            */", cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);

            var newMemberInitiationRoot = await CSharpSyntaxTree.ParseText("new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext()", cancellationToken: cancellationToken)
                .GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newMemberInitiation = newMemberInitiationRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            var newMemberInitiationWithComment = newMemberInitiation.WithLeadingTrivia(comment.GetLeadingTrivia());

            return document.WithSyntaxRoot(oldRoot!.ReplaceNode(invocation, newMemberInitiationWithComment));
        }
    }
}
