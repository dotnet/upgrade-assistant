// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class AddAdapterDescriptorCodeFixer : CodeFixProvider
    {
        private const string NamespaceName = "Microsoft.CodeAnalysis.Refactoring";
        private const string AttributeName = "AdapterDescriptorAttribute";
        private const string FullAttributeName = NamespaceName + "." + AttributeName;
        private const string SystemType = "System.Type";
        private const string SystemAttribute = "System.Attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MissingAdapterDescriptor.AddAdapterDescriptorDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            // Multiple documents don't seem to merge well
            return null!;
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

            var node = root.FindNode(context.Span);

            if (semantic.GetSymbolInfo(node, context.CancellationToken).Symbol is not ITypeSymbol type)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AddAdapterDescriptorTitle,
                    async cancellationToken =>
                    {
                        var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                        var project = context.Document.Project;

                        if (semantic.Compilation.GetTypeByMetadataName(FullAttributeName) is null)
                        {
                            project = project.AddDocument($"{AttributeName}.cs", CreateDescriptorAttribute(editor.Generator))
                                .Project;
                        }

                        project = project.AddDocument("Descriptors.cs", CreateAttributeInstance(editor.Generator, type))
                            .Project;

                        return project.Solution;
                    },
                    nameof(AddAdapterDescriptorCodeFixer)),
                context.Diagnostics);
        }

        private static SyntaxNode CreateAttributeInstance(SyntaxGenerator generator, ITypeSymbol type) =>
            generator.AddAttributes(
                generator.CompilationUnit(),
                generator.Attribute(
                    generator.DottedName(FullAttributeName),
                    new[] { generator.TypeOfExpression(generator.NameExpression(type)) }))
                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                .WithAdditionalAnnotations(Simplifier.Annotation);

        private static SyntaxNode CreateDescriptorAttribute(SyntaxGenerator generator)
        {
            var constructor = generator.ConstructorDeclaration(
                parameters: new[]
                {
                    generator.ParameterDeclaration("original", generator.DottedName(SystemType)),
                    generator.ParameterDeclaration("interfaceType", generator.DottedName(SystemType), initializer: generator.NullLiteralExpression()),
                },
                accessibility: Accessibility.Public);
            var type = generator.ClassDeclaration(
                AttributeName,
                modifiers: DeclarationModifiers.Sealed,
                accessibility: Accessibility.Internal,
                baseType: generator.DottedName(SystemAttribute),
                members: new[] { constructor });

            var target = generator.MemberAccessExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("System"),
                    "AttributeTargets"),
                "Assembly");

            var typeWithUsage = generator.AddAttributes(
                type,
                generator.Attribute("System.AttributeUsageAttribute",
                    generator.Argument(target),
                    generator.AttributeArgument("AllowMultiple",
                        generator.TrueLiteralExpression())))
                .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            return generator.CompilationUnit(
                generator.AddMembers(
                    generator.NamespaceDeclaration(NamespaceName),
                    typeWithUsage));
        }
    }
}
