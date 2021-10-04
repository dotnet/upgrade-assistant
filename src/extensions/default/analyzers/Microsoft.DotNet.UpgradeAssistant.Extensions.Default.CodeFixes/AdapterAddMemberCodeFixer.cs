// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AdapterAddMemberCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AdapterRefactorAnalyzer.AddMemberDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];

            if (AdapterDescriptor.TryGetExpectedType(diagnostic.Properties, out var typeString) && AdapterDescriptor.TryGetMethod(diagnostic.Properties, out var methodString))
            {
                var semantic = await context.Document.GetSemanticModelAsync();

                if (semantic is null)
                {
                    return;
                }

                var type = semantic.Compilation.GetTypeByMetadataName(typeString);

                if (type is null)
                {
                    return;
                }

                var syntax = type.Locations.FirstOrDefault();

                if (syntax is null)
                {
                    return;
                }

                if (!syntax.IsInSource)
                {
                    return;
                }

                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

                if (root is null)
                {
                    return;
                }

                var node = root.FindNode(syntax.SourceSpan);
                var methodDeclaration = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseMemberDeclaration(methodString);

                if (methodDeclaration is null)
                {
                    return;
                }

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AdapterRefactorTitle,
                        async cancellationToken =>
                        {
                            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);

                            var exp = methodDeclaration
                                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                                .WithAdditionalAnnotations(Simplifier.Annotation);
                            editor.AddMember(node, exp);

                            return editor.GetChangedDocument();
                        },
                        nameof(CodeFixResources.UsingSystemWebTitle)),
                    context.Diagnostics);
            }
        }
    }
}
