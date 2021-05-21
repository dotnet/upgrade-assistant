// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
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

            //// Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.HttpContextRefactorTitle,
                    createChangedSolution: c => InjectHttpContext(context.Document, semantic, methodOperation, property, c),
                    equivalenceKey: nameof(CodeFixResources.HttpContextRefactorTitle)),
                diagnostic);
        }

        private static async Task<Solution> InjectHttpContext(Document document, SemanticModel model, IOperation methodOperation, IPropertyReferenceOperation propertyOperation, CancellationToken cancellationToken)
        {
            var slnEditor = new SolutionEditor(document.Project.Solution);

            var editor = await slnEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

            // Add parameter if not available
            var parameter = GetOrAddMethodParameter(model, editor, methodOperation, propertyOperation, cancellationToken);

            if (parameter is null)
            {
                return document.Project.Solution;
            }

            // Update node usage
            editor.ReplaceNode(propertyOperation.Syntax, parameter);

            if (methodOperation.SemanticModel?.GetDeclaredSymbol(methodOperation.Syntax, cancellationToken) is ISymbol methodSymbol)
            {
                await UpdateCallersAsync(methodOperation.SemanticModel, methodSymbol, propertyOperation.Property, slnEditor, cancellationToken).ConfigureAwait(false);
            }

            return slnEditor.GetChangedSolution();
        }

        private static SyntaxNode? GetOrAddMethodParameter(SemanticModel semanticModel, DocumentEditor editor, IOperation methodOperation, IPropertyReferenceOperation propertyOperation, CancellationToken token)
        {
            // Search to see if an existing expression can be used (ie an existing parameter or property that matches the type)
            var expression = GetExistingExpression(semanticModel, propertyOperation.Property, editor, propertyOperation.Syntax, token);

            if (expression is not null)
            {
                return expression;
            }

            var propertyTypeSyntaxNode = editor.Generator.NameExpression(propertyOperation.Property.Type);
            var name = GetArgumentName();

            var p = editor.Generator.ParameterDeclaration(name, propertyTypeSyntaxNode);

            editor.AddParameter(methodOperation.Syntax, p);

            return editor.Generator.IdentifierName(name);

            string GetArgumentName()
            {
                var symbol = semanticModel.GetDeclaredSymbol(methodOperation.Syntax, cancellationToken: token);

                if (symbol is not IMethodSymbol method)
                {
                    return DefaultArgumentName;
                }

                var set = new HashSet<string>(symbol.GetStringComparer());

                foreach (var p in method.Parameters)
                {
                    set.Add(p.Name);
                }

                var i = 0;
                while (true)
                {
                    var name = ++i == 1 ? DefaultArgumentName : $"{DefaultArgumentName}{i}";

                    if (!set.Contains(name))
                    {
                        return name;
                    }
                }
            }
        }

        private static async Task UpdateCallersAsync(SemanticModel semanticModel, ISymbol methodSymbol, IPropertySymbol property, SolutionEditor slnEditor, CancellationToken token)
        {
            // Check callers
            var callers = await SymbolFinder.FindCallersAsync(methodSymbol, slnEditor.OriginalSolution, token).ConfigureAwait(false);

            foreach (var caller in callers)
            {
                var location = caller.Locations.FirstOrDefault();

                if (location is null)
                {
                    continue;
                }

                if (!slnEditor.OriginalSolution.TryGetDocument(location.SourceTree, out var document))
                {
                    continue;
                }

                var editor = await slnEditor.GetDocumentEditorAsync(document.Id, token).ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);

                if (root is null)
                {
                    continue;
                }

                var callerNode = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

                if (callerNode is null)
                {
                    continue;
                }

                var invocationNode = callerNode.GetInvocationExpression();

                if (invocationNode is not null)
                {
                    var expression = GetExistingExpression(semanticModel, property, editor, invocationNode, token) ?? CreateDefaultParameter();
                    var argument = editor.Generator.Argument(expression);
                    var newInvocation = invocationNode.AddArgumentToInvocation(argument);

                    editor.ReplaceNode(invocationNode, newInvocation);

                    SyntaxNode CreateDefaultParameter()
                    {
                        var httpContextType = editor.Generator.NameExpression(property.Type);
                        return editor.Generator.MemberAccessExpression(httpContextType, "Current");
                    }
                }
            }
        }

        private static string? GetPropertyName(SemanticModel semanticModel, ITypeSymbol type, SyntaxNode node, CancellationToken token)
        {
            var operation = semanticModel.GetOperation(node, token);
            var methodOperation = operation.GetEnclosingMethodOperation();

            if (methodOperation is null)
            {
                return null;
            }

            var symbol = semanticModel.GetDeclaredSymbol(methodOperation.Syntax, cancellationToken: token);

            if (symbol?.ContainingType is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            foreach (var member in GetAllMembers(typeSymbol, !symbol.IsStatic))
            {
                if (member is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, type))
                {
                    return property.Name;
                }
                else if (member is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.Type, type))
                {
                    return field.Name;
                }
            }

            return null;
        }

        private static IEnumerable<ISymbol> GetAllMembers(INamedTypeSymbol? symbol, bool includeInstance)
        {
            var allowPrivate = true;

            while (symbol is not null)
            {
                foreach (var member in symbol.GetMembers())
                {
                    if (member.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    if (!includeInstance && !member.IsStatic)
                    {
                        continue;
                    }

                    if (!allowPrivate && member.DeclaredAccessibility == Accessibility.Private)
                    {
                        continue;
                    }

                    yield return member;
                }

                // After the first level, we cannot see private
                allowPrivate = false;

                symbol = symbol.BaseType;
            }
        }

        private static string? GetEnclosingMethodParameterName(SemanticModel semanticModel, ITypeSymbol type, SyntaxNode node, CancellationToken token)
        {
            var operation = semanticModel.GetOperation(node, token);
            var methodOperation = operation.GetEnclosingMethodOperation();

            if (methodOperation is null)
            {
                return null;
            }

            var parameter = methodOperation.Syntax.GetExistingParameterSymbol(semanticModel, type, token);

            return parameter?.Name;
        }

        private static SyntaxNode? GetExistingExpression(SemanticModel model, IPropertySymbol property, SyntaxEditor editor, SyntaxNode invocation, CancellationToken token)
        {
            return GetParameterFromMethod() ?? GetParameterFromProperty();

            SyntaxNode? GetParameterFromMethod()
            {
                var name = GetEnclosingMethodParameterName(model, property.Type, invocation, token);

                if (name is not null)
                {
                    return editor.Generator.IdentifierName(name);
                }

                return null;
            }

            SyntaxNode? GetParameterFromProperty()
            {
                var name = GetPropertyName(model, property.Type, invocation, token);

                if (name is not null)
                {
                    return editor.Generator.IdentifierName(name);
                }

                return null;
            }
        }
    }
}
