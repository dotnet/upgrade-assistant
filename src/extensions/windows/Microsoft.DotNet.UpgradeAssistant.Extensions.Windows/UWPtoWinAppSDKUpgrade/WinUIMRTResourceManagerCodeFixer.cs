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
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(WinUIMRTResourceManagerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
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

            var declaration = root.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    FixableDiagnosticIds.First(),
                    c => FixResourceManagerAPI(context.Document, declaration, c),
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
    }
}
