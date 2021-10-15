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
    public class AdapterDefinitionCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AdapterDefinitionAnalyzer.DefinitionDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            // Multiple documents don't seem to merge well
            return null!;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var semantic = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);

            if (semantic is null)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan);

            if (node is null)
            {
                return;
            }

            if (diagnostic.Properties.TryGetTypeToReplace(semantic, out var typeToReplace))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeFixResources.AdapterDefinitionTitle,
                        createChangedSolution: async cancellationToken =>
                        {
                            // Update attribute
                            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                            var interfaceName = $"I{typeToReplace.Name}";
                            var defaultNamespace = context.Document.Project.DefaultNamespace ?? context.Document.Project.Name;
                            var fullyQualifiedInterfaceName = $"{defaultNamespace}.{interfaceName}";

                            var newArg = editor.Generator.AttributeArgument(
                                editor.Generator.TypeOfExpression(
                                    editor.Generator.QualifiedName(
                                        editor.Generator.IdentifierName(defaultNamespace),
                                        editor.Generator.IdentifierName(interfaceName))));

                            editor.AddAttributeArgument(node, newArg);

                            var project = editor.GetChangedDocument().Project;

                            if (semantic.Compilation.GetTypeByMetadataName(fullyQualifiedInterfaceName) is not null)
                            {
                                return project.Solution;
                            }

                            var interfaceDeclaration = editor.Generator.InterfaceDeclaration(interfaceName, accessibility: Accessibility.Public);
                            var namespaceDeclaration = editor.Generator.NamespaceDeclaration(
                                editor.Generator.IdentifierName(defaultNamespace),
                                interfaceDeclaration);

                            // Add the interface declaration to the abstractions project
                            var addedDocument = project.AddDocument($"{interfaceName}.cs", editor.Generator.CompilationUnit(namespaceDeclaration));

                            return addedDocument.Project.Solution;
                        },
                        nameof(AdapterDefinitionCodeFixer)),
                    diagnostic);
            }
        }
    }
}
