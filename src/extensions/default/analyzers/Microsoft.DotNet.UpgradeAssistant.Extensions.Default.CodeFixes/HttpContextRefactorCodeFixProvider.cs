// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(HttpContextRefactorCodeFixProvider))]
    [Shared]
    public class HttpContextRefactorCodeFixProvider : CodeFixProvider
    {
        private const string DefaultArgumentName = "currentContext";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(HttpContextCurrentAnalyzer.DiagnosticId); }
        }

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
            var semantic = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            if (semantic is null)
            {
                return;
            }

            // Find the type declaration identified by the diagnostic.
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            if (semantic.GetOperation(node, context.CancellationToken) is not IPropertyReferenceOperation property)
            {
                return;
            }

            var methodOperation = property.GetEnclosingMethodOperation();

            if (methodOperation is null)
            {
                return;
            }

            var methodSymbol = methodOperation.SemanticModel!.GetDeclaredSymbol(methodOperation.Syntax, context.CancellationToken) as IMethodSymbol;

            if (methodSymbol is null)
            {
                return;
            }

            //// Register a code action that will invoke the fix.
            context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.HttpContextRefactorTitle,
                createChangedSolution: async c =>
                {
                    var slnEditor = new SolutionEditor(context.Document.Project.Solution);
                    var docEditor = await slnEditor.GetDocumentEditorAsync(context.Document.Id, c).ConfigureAwait(false);
                    var injector = new StaticCallToMethodInjector(slnEditor, docEditor, methodOperation, methodSymbol, property, DefaultArgumentName);

                    await injector.MethodInjectPropertyAsync(c).ConfigureAwait(false);

                    return slnEditor.GetChangedSolution();
                },
                equivalenceKey: nameof(CodeFixResources.HttpContextRefactorTitle)),
            diagnostic);
        }
    }
}
