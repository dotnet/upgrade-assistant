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

            var injector = new HttpContextMethodInjector(context.Document, semantic, methodOperation, property);

            //// Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.HttpContextRefactorTitle,
                    createChangedSolution: c => injector.InjectHttpContext(c),
                    equivalenceKey: nameof(CodeFixResources.HttpContextRefactorTitle)),
                diagnostic);
        }

        private record HttpContextMethodInjector
        {
            private Document Document { get; init; }

            private SemanticModel Model { get; init; }

            private IOperation EnclosingMethodOperation { get; init; }

            private IPropertyReferenceOperation PropertyOperation { get; init; }

            public HttpContextMethodInjector(
                Document document,
                SemanticModel model,
                IOperation methodOPeration,
                IPropertyReferenceOperation propertyOperation)
            {
                Document = document;
                Model = model;
                EnclosingMethodOperation = methodOPeration;
                PropertyOperation = propertyOperation;
            }

            private IMethodSymbol? methodSymbol;

            private IMethodSymbol? GetEnclosingMethodSymbol(CancellationToken token)
            {
                if (methodSymbol is null)
                {
                    methodSymbol = Model.GetDeclaredSymbol(EnclosingMethodOperation.Syntax, token) as IMethodSymbol;
                }

                return methodSymbol;
            }

            public async Task<Solution> InjectHttpContext(CancellationToken token)
            {
                var slnEditor = new SolutionEditor(Document.Project.Solution);
                var editor = await slnEditor.GetDocumentEditorAsync(Document.Id, token).ConfigureAwait(false);

                // Add parameter if not available
                var parameter = GetOrAddMethodParameter(editor, token);
                if (parameter is null)
                {
                    return Document.Project.Solution;
                }

                // Update node usage
                editor.ReplaceNode(PropertyOperation.Syntax, parameter);
                await UpdateCallersAsync(GetEnclosingMethodSymbol(token), slnEditor, token).ConfigureAwait(false);

                return slnEditor.GetChangedSolution();
            }

            private SyntaxNode? GetOrAddMethodParameter(DocumentEditor editor, CancellationToken token)
            {
                // Search to see if an existing expression can be used (ie an existing parameter or property that matches the type)
                var expression = GetExistingExpression(editor.Generator, PropertyOperation.Syntax, token);

                if (expression is not null)
                {
                    return expression;
                }

                var propertyTypeSyntaxNode = editor.Generator.NameExpression(PropertyOperation.Property.Type);
                var name = GetArgumentName(token);

                var p = editor.Generator.ParameterDeclaration(name, propertyTypeSyntaxNode);

                editor.AddParameter(EnclosingMethodOperation.Syntax, p);

                return editor.Generator.IdentifierName(name);
            }

            private string? _argumentName;

            private string GetArgumentName(CancellationToken token)
            {
                if (_argumentName is not null)
                {
                    return _argumentName;
                }

                var symbol = GetEnclosingMethodSymbol(token);

                if (symbol is not IMethodSymbol method)
                {
                    return _argumentName = DefaultArgumentName;
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
                        return _argumentName = name;
                    }
                }
            }

            private async Task UpdateCallersAsync(ISymbol? methodSymbol, SolutionEditor slnEditor, CancellationToken token)
            {
                if (methodSymbol is null)
                {
                    return;
                }

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
                        var editor = await slnEditor.GetDocumentEditorAsync(document.Id, token).ConfigureAwait(false);
                        var expression = GetExistingExpression(editor.Generator, invocationNode, token) ?? CreateDefaultParameter();
                        var argument = editor.Generator.Argument(expression);
                        var newInvocation = invocationNode.AddArgumentToInvocation(argument);

                        editor.ReplaceNode(invocationNode, newInvocation);

                        SyntaxNode CreateDefaultParameter()
                        {
                            var httpContextType = editor.Generator.NameExpression(PropertyOperation.Property.Type);
                            return editor.Generator.MemberAccessExpression(httpContextType, "Current");
                        }
                    }
                }
            }

            private string? GetEnclosingMethodParameterName(SyntaxNode node, CancellationToken token)
            {
                var operation = Model.GetOperation(node, token);
                var methodOperation = operation.GetEnclosingMethodOperation();

                if (methodOperation is null)
                {
                    return null;
                }

                var parameter = methodOperation.Syntax.GetExistingParameterSymbol(Model, PropertyOperation.Property.Type, token);

                return parameter?.Name;
            }

            private SyntaxNode? GetExistingExpression(SyntaxGenerator generator, SyntaxNode invocation, CancellationToken token)
            {
                return GetParameterFromMethod() ?? GetParameterFromProperty();

                SyntaxNode? GetParameterFromMethod()
                {
                    var name = GetEnclosingMethodParameterName(invocation, token);

                    if (name is not null)
                    {
                        return generator.IdentifierName(name);
                    }

                    return null;
                }

                SyntaxNode? GetParameterFromProperty()
                {
                    var operation = Model.GetOperation(invocation, token);
                    var methodOperation = operation.GetEnclosingMethodOperation();

                    if (methodOperation is null)
                    {
                        return null;
                    }

                    var symbol = Model.GetDeclaredSymbol(methodOperation.Syntax, cancellationToken: token);

                    if (symbol?.ContainingType is not INamedTypeSymbol typeSymbol)
                    {
                        return null;
                    }

                    foreach (var member in typeSymbol.GetAllMembers(!symbol.IsStatic))
                    {
                        if (member is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, PropertyOperation.Property.Type))
                        {
                            return generator.IdentifierName(property.Name);
                        }
                        else if (member is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.Type, PropertyOperation.Property.Type))
                        {
                            return generator.IdentifierName(field.Name);
                        }
                    }

                    return null;
                }
            }
        }
    }
}
