// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes
{
    [ApplicableComponents(ProjectComponents.Web)]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA006 CodeFix Provider")]
    public class HttpContextIsDebuggingEnabledCodeFixer : CodeFixProvider
    {
        private const string DiagnosticsNamespace = "System.Diagnostics";
        private const string DebuggerIsAttachedSyntax = "System.Diagnostics.Debugger.IsAttached";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HttpContextIsDebuggingEnabledAnalyzer.DiagnosticId);

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

            var node = root.FindNode(context.Span);

            if (node is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.HttpContextIsDebuggingEnabled,
                    cancellationToken => UpdateMemberAccessAsync(context.Document, node, cancellationToken),
                    nameof(CodeFixResources.HttpContextIsDebuggingEnabled)),
                context.Diagnostics);
        }

        private static async Task<Document> UpdateMemberAccessAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            if (editor.OriginalRoot is not CompilationUnitSyntax documentRoot)
            {
                return document;
            }

            // Replace the member access expression with Debugger.IsAttached
            var newExpression = SyntaxFactory.ParseExpression(DebuggerIsAttachedSyntax)
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation);
            documentRoot = documentRoot.ReplaceNode(node, newExpression)!;

            // Add a using statement to System.Diagnostics, if necessary
            documentRoot = documentRoot.AddUsingIfMissing(DiagnosticsNamespace);

            editor.ReplaceNode(editor.OriginalRoot, documentRoot);
            return editor.GetChangedDocument();
        }
    }
}
