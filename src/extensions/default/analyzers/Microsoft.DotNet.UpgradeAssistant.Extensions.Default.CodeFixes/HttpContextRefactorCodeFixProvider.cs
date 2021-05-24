// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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

            var semantic = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            if (semantic is null)
            {
                return;
            }

            var node = root.FindNode(context.Span, getInnermostNodeForTie: true);

            if (semantic.GetOperation(node, context.CancellationToken) is not IPropertyReferenceOperation property)
            {
                return;
            }

            var methodOperation = property.GetEnclosingMethodOperation();

            if (methodOperation is null)
            {
                return;
            }

            if (semantic.GetDeclaredSymbol(methodOperation.Syntax, context.CancellationToken) is not IMethodSymbol methodSymbol)
            {
                return;
            }

            //// Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.HttpContextRefactorTitle,
                    createChangedSolution: c => context.Document.MovePropertyAccessToMethodInjectionAsync(property, methodOperation, methodSymbol, DefaultArgumentName, c),
                    equivalenceKey: nameof(CodeFixResources.HttpContextRefactorTitle)),
                context.Diagnostics);
        }
    }
}
