// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AdapterCallFactoryCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AdapterRefactorAnalyzer.CallFactoryDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            // TODO: Encounters an InvalidCastException if WellKnownFixAllProviders.BatchFixer is used
            return null!;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];

            var semantic = await context.Document.GetSemanticModelAsync();

            if (semantic is null)
            {
                return;
            }

            var adapterContext = AdapterContext.Create().FromCompilation(semantic.Compilation);

            if (diagnostic.Properties.TryGetExpectedType(semantic, out var type))
            {
                var factory = adapterContext.GetFactory(type);

                if (factory is null)
                {
                    return;
                }

                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

                if (root is null)
                {
                    return;
                }

                var node = root.FindNode(diagnostic.Location.SourceSpan);

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AdapterCallFactoryTitle,
                        createChangedDocument: async cancellationToken =>
                        {
                            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);

                            var memberAccess = editor.Generator.MemberAccessExpression(
                                editor.Generator.NameExpression(factory.ContainingType),
                                factory.Name);
                            var invocation = editor.Generator.InvocationExpression(memberAccess, node);
                            var arg = editor.Generator.Argument(invocation);

                            editor.ReplaceNode(node, arg);

                            return editor.GetChangedDocument();
                        },
                        equivalenceKey: factory.ToDisplayString()),
                    context.Diagnostics);
            }
        }
    }
}
