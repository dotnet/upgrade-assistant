// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    internal static class MethodInjectionExtensions
    {
        /// <summary>
        /// Provides a helper to raise a static property reference to an argument injected into the method.
        /// </summary>
        /// <param name="document">The document the property reference occurs in.</param>
        /// <param name="propertyReferenceOperation">The property reference operation.</param>
        /// <param name="methodOperation">The method operation the property reference occurs in.</param>
        /// <param name="methodSymbol">The method symbol where the property reference occurs.</param>
        /// <param name="defaultArgumentName">The default argument name that is injected.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>An updated solution with the changes.</returns>
        public static async Task<Solution> MovePropertyAccessToMethodInjectionAsync(
            this Document document,
            IPropertyReferenceOperation propertyReferenceOperation,
            IOperation methodOperation,
            IMethodSymbol methodSymbol,
            string defaultArgumentName,
            CancellationToken token)
        {
            var slnEditor = new SolutionEditor(document.Project.Solution);
            var docEditor = await slnEditor.GetDocumentEditorAsync(document.Id, token).ConfigureAwait(false);
            var injector = new StaticCallToMethodInjector(slnEditor, docEditor, methodOperation, methodSymbol, propertyReferenceOperation, defaultArgumentName);

            await injector.MethodInjectPropertyAsync(token).ConfigureAwait(false);

            return slnEditor.GetChangedSolution();
        }

        /// <summary>
        /// Converts a static property call, such as HttpContext.Current, to inject the type via method injections.
        /// </summary>
        private record StaticCallToMethodInjector
        {
            private readonly string _defaultArgumentName;

            public StaticCallToMethodInjector(
                SolutionEditor helper,
                DocumentEditor editor,
                IOperation containingMethodOperation,
                IMethodSymbol containingMethodSymbol,
                IPropertyReferenceOperation propertyOperation,
                string defaultArgName)
            {
                DocEditor = editor;
                SlnEditor = helper;
                ContainingMethodSymbol = containingMethodSymbol;
                ContainingMethodOperation = containingMethodOperation;
                PropertyOperation = propertyOperation;
                _defaultArgumentName = defaultArgName;
            }

            public SolutionEditor SlnEditor { get; }

            public DocumentEditor DocEditor { get; init; }

            private IOperation ContainingMethodOperation { get; }

            private IPropertyReferenceOperation PropertyOperation { get; }

            private IMethodSymbol ContainingMethodSymbol { get; }

            public async Task MethodInjectPropertyAsync(CancellationToken token)
            {
                // Add parameter if not available
                var parameter = GetOrAddMethodParameter(token);
                if (parameter is null)
                {
                    return;
                }

                // Update node usage
                DocEditor.ReplaceNode(PropertyOperation.Syntax, parameter);
                await UpdateCallersAsync(token).ConfigureAwait(false);
            }

            /// <summary>
            /// Gets an existing method parameter or creates a new one of the property type.
            /// </summary>
            private SyntaxNode? GetOrAddMethodParameter(CancellationToken token)
            {
                // Search to see if an existing expression can be used (ie an existing parameter or property that matches the type)
                var expression = GetExistingVariableOrMember(PropertyOperation.Syntax, token);

                if (expression is not null)
                {
                    return expression;
                }

                var propertyTypeSyntaxNode = DocEditor.Generator.NameExpression(PropertyOperation.Property.Type);
                var name = GetArgumentName();

                DocEditor.AddParameter(ContainingMethodOperation.Syntax,
                    DocEditor.Generator.ParameterDeclaration(name, propertyTypeSyntaxNode));

                return DocEditor.Generator.IdentifierName(name);

                // Ensures we don't have any collision with other existing names
                string GetArgumentName()
                {
                    var i = 0;
                    while (true)
                    {
                        var name = ++i == 1 ? _defaultArgumentName : $"{_defaultArgumentName}{i}";
                        var exists = ContainingMethodSymbol.Parameters.Any(p => p.NameEquals(name));

                        if (!exists)
                        {
                            return name;
                        }
                    }
                }
            }

            private async Task UpdateCallersAsync(CancellationToken token)
            {
                var callers = await SymbolFinder.FindCallersAsync(ContainingMethodSymbol, SlnEditor.OriginalSolution, token).ConfigureAwait(false);

                foreach (var caller in callers)
                {
                    foreach (var location in caller.Locations)
                    {
                        if (location is null)
                        {
                            continue;
                        }

                        if (!location.IsInSource)
                        {
                            continue;
                        }

                        if (!SlnEditor.OriginalSolution.TryGetDocument(location.SourceTree, out var doc))
                        {
                            continue;
                        }

                        var injector = this with
                        {
                            DocEditor = await SlnEditor.GetDocumentEditorAsync(doc.Id, token).ConfigureAwait(false)
                        };

                        var root = await injector.DocEditor.OriginalDocument.GetSyntaxRootAsync(token).ConfigureAwait(false);

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
                            injector.UpdateInvocation(invocationNode, token);
                        }
                    }
                }
            }

            /// <summary>
            /// Updates an invocation to either use existing variables in scope, or to call the original property.
            /// </summary>
            private void UpdateInvocation(SyntaxNode invocationNode, CancellationToken token)
            {
                var expression = GetExistingVariableOrMember(invocationNode, token) ?? CreateDefaultParameter();
                var argument = DocEditor.Generator.Argument(expression);
                var newInvocation = invocationNode.AddArgumentToInvocation(argument);

                DocEditor.ReplaceNode(invocationNode, newInvocation);

                SyntaxNode CreateDefaultParameter()
                {
                    var propertyType = DocEditor.Generator.NameExpression(PropertyOperation.Property.Type);
                    return DocEditor.Generator.MemberAccessExpression(propertyType, PropertyOperation.Property.Name);
                }
            }

            private SyntaxNode? GetExistingVariableOrMember(SyntaxNode node, CancellationToken token)
            {
                return FindAvailableMethodParameter() ?? FindAvailableMember();

                SyntaxNode? FindAvailableMethodParameter()
                {
                    var dataFlow = DocEditor.SemanticModel.AnalyzeDataFlow(node);

                    foreach (var symbol in dataFlow.DefinitelyAssignedOnEntry)
                    {
                        if (symbol is IParameterSymbol p && SymbolEqualityComparer.Default.Equals(p.Type, PropertyOperation.Property.Type))
                        {
                            return DocEditor.Generator.IdentifierName(p.Name);
                        }
                    }

                    return null;
                }

                SyntaxNode? FindAvailableMember()
                {
                    var operation = DocEditor.SemanticModel.GetOperation(node, token);
                    var methodOperation = operation.GetEnclosingMethodOperation();

                    if (methodOperation is null)
                    {
                        return null;
                    }

                    var symbol = DocEditor.SemanticModel.GetDeclaredSymbol(methodOperation.Syntax, cancellationToken: token);

                    if (symbol?.ContainingType is not INamedTypeSymbol typeSymbol)
                    {
                        return null;
                    }

                    foreach (var member in typeSymbol.GetAllMembers())
                    {
                        if (symbol.IsStatic)
                        {
                            continue;
                        }

                        if (member is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, PropertyOperation.Property.Type))
                        {
                            return DocEditor.Generator.IdentifierName(property.Name);
                        }
                        else if (member is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.Type, PropertyOperation.Property.Type))
                        {
                            return DocEditor.Generator.IdentifierName(field.Name);
                        }
                    }

                    return null;
                }
            }
        }
    }
}
