// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AdapterStaticMemberCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AdapterRefactorAnalyzer.StaticMemberDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var semantic = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);

            if (semantic is null)
            {
                return;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            if (node is null)
            {
                return;
            }

            if (diagnostic.Properties.TryGetMissingMember(semantic, out var member) && member is IPropertySymbol property)
            {
                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AdapterStaticMemberTitle,
                        createChangedDocument: async cancellationToken =>
                        {
                            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                            var propertyAccess = editor.Generator.MemberAccessExpression(
                                editor.Generator.NameExpression(property.ContainingType),
                                property.Name)
                                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                                .WithAdditionalAnnotations(Simplifier.Annotation);
                            editor.ReplaceNode(node, propertyAccess);

                            return editor.GetChangedDocument();
                        },
                        nameof(AdapterStaticMemberCodeFixer)),
                    context.Diagnostics);
            }
        }
    }
}
