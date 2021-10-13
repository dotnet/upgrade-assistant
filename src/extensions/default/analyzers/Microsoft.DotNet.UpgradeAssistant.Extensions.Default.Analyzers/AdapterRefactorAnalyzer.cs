// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AdapterRefactorAnalyzer : DiagnosticAnalyzer
    {
        public const string RefactorDiagnosticId = "UA0014";
        public const string AddMemberDiagnosticId = "UA0014b";
        public const string CallFactoryDiagnosticId = "UA0014c";

        private const string Category = "Refactor";

        private static readonly LocalizableString RefactorTitle = new LocalizableResourceString(nameof(Resources.AdapterRefactorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString RefactorMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterRefactorMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString RefactorDescription = new LocalizableResourceString(nameof(Resources.AdapterRefactorDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AddMemberTitle = new LocalizableResourceString(nameof(Resources.AdapterAddMemberTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddMemberMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterAddMemberMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddMemberDescription = new LocalizableResourceString(nameof(Resources.AdapterAddMemberDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString CallFactoryTitle = new LocalizableResourceString(nameof(Resources.AdapterCallFactoryTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString CallFactoryMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterCallFactoryMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString CallFactoryDescription = new LocalizableResourceString(nameof(Resources.AdapterCallFactoryDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor RefactorRule = new(RefactorDiagnosticId, RefactorTitle, RefactorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: RefactorDescription);
        private static readonly DiagnosticDescriptor AddMemberRule = new(AddMemberDiagnosticId, AddMemberTitle, AddMemberMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AddMemberDescription);
        private static readonly DiagnosticDescriptor CallFactoryRule = new(CallFactoryDiagnosticId, CallFactoryTitle, CallFactoryMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: CallFactoryDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(RefactorRule, AddMemberRule, CallFactoryRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var adapterContext = AdapterContext.Create().FromCompilation(context.Compilation);

                if (!adapterContext.IsAvailable)
                {
                    return;
                }

                RegisterAddMemberActions(adapterContext, context);
                RegisterAdapterActions(adapterContext, context);
                RegisterCallFactoryActions(adapterContext, context);
            });
        }

        private static void RegisterAddMemberActions(AdapterContext adapterContext, CompilationStartAnalysisContext context)
        {
            context.RegisterOperationAction(context =>
            {
                if (context.Operation.GetEnclosingMethod() is IMethodSymbol enclosing && adapterContext.IsFactoryMethod(enclosing))
                {
                    return;
                }

                var member = context.Operation switch
                {
                    IInvocationOperation invocation => (ISymbol)invocation.TargetMethod,
                    IPropertyReferenceOperation property => property.Property,
                    _ => throw new NotImplementedException(),
                };

                foreach (var adapter in adapterContext.Descriptors)
                {
                    if (SymbolEqualityComparer.Default.Equals(member.ContainingType, adapter.Original))
                    {
                        // TODO: this could be better by matching if it actually binds
                        if (adapter.Destination.GetMembers(member.Name).Length == 0)
                        {
                            var properties = adapter.Properties
                                .WithMissingMember(member);

                            context.ReportDiagnostic(Diagnostic.Create(AddMemberRule, context.Operation.Syntax.GetLocation(), properties: properties, member.Name, adapter.Destination));
                        }
                    }
                }
            }, OperationKind.Invocation, OperationKind.PropertyReference);
        }

        private static void RegisterAdapterActions(AdapterContext adapterContext, CompilationStartAnalysisContext context)
        {
            // TODO: Can we use a symbol/operation action? Need to be more exhaustive on search for types
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                context.RegisterSyntaxNodeAction(context =>
                {
                    if (context.Node is CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method)
                    {
                        var symbol = context.SemanticModel.GetDeclaredSymbol(method);

                        if (!adapterContext.Ignore(symbol))
                        {
                            GeneralizedSyntaxNodeAction(context, adapterContext, method.ReturnType);
                            GeneralizedParameterAction(context, adapterContext, method.ParameterList.Parameters, static n => n.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax field)
                    {
                        GeneralizedSyntaxNodeAction(context, adapterContext, field.Declaration.Type);
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property)
                    {
                        GeneralizedSyntaxNodeAction(context, adapterContext, property.Type);
                    }
                }, CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration, CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration);
            }
            else if (context.Compilation.Language == LanguageNames.VisualBasic)
            {
                context.RegisterSyntaxNodeAction(context =>
                {
                    if (context.Node is CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax method)
                    {
                        GeneralizedSyntaxNodeAction(context, adapterContext, method.AsClause?.Type);
                        GeneralizedParameterAction(context, adapterContext, method.ParameterList.Parameters, static n => n.AsClause?.Type);
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax property)
                    {
                        if (property.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                        {
                            GeneralizedSyntaxNodeAction(context, adapterContext, simple.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.FieldDeclarationSyntax field)
                    {
                        foreach (var declarator in field.Declarators)
                        {
                            if (declarator.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                            {
                                GeneralizedSyntaxNodeAction(context, adapterContext, simple.Type);
                            }
                        }
                    }
                }, CodeAnalysis.VisualBasic.SyntaxKind.FunctionStatement, CodeAnalysis.VisualBasic.SyntaxKind.SubStatement, CodeAnalysis.VisualBasic.SyntaxKind.FieldDeclaration, CodeAnalysis.VisualBasic.SyntaxKind.PropertyStatement);
            }
        }

        private static void RegisterCallFactoryActions(AdapterContext adapterContext, CompilationStartAnalysisContext context)
        {
            if (adapterContext.Factories.Length == 0)
            {
                return;
            }

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvalidOperation)context.Operation;
                var symbolInfo = context.Operation.SemanticModel.GetSymbolInfo(operation.Syntax);

                foreach (var child in operation.Children)
                {
                    if (child is IParameterReferenceOperation parameter)
                    {
                        foreach (var descriptor in adapterContext.Descriptors)
                        {
                            if (SymbolEqualityComparer.Default.Equals(parameter.Type, descriptor.Original))
                            {
                                // TODO: should match arguments, but seems non-trivial
                                foreach (var proposedMethod in symbolInfo.CandidateSymbols)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(CallFactoryRule, parameter.Syntax.GetLocation(), properties: descriptor.Properties, descriptor.OriginalMessage, descriptor.DestinationMessage));
                                }
                            }
                        }
                    }
                }
            }, OperationKind.Invalid);
        }

        private static void GeneralizedSyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            AdapterContext adapterContext,
            SyntaxNode? syntaxNode)
        {
            if (syntaxNode is null)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(syntaxNode).Symbol;

            if (symbol is null)
            {
                return;
            }

            foreach (var adapter in adapterContext.Descriptors)
            {
                if (SymbolEqualityComparer.Default.Equals(adapter.Original, symbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RefactorRule, syntaxNode.GetLocation(), properties: adapter.Properties, adapter.OriginalMessage, adapter.DestinationMessage));
                }
            }
        }

        private static void GeneralizedParameterAction<TParameter>(
            SyntaxNodeAnalysisContext context,
            AdapterContext adapterContext,
            SeparatedSyntaxList<TParameter> parameters,
            Func<TParameter, SyntaxNode?> parameterToType)
            where TParameter : SyntaxNode
        {
            var method = context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (method is null)
            {
                return;
            }

            foreach (var adapter in adapterContext.Descriptors)
            {
                foreach (var p in parameters)
                {
                    if (parameterToType(p) is SyntaxNode type && context.SemanticModel.GetSymbolInfo(type) is SymbolInfo info && info.Symbol is ISymbol parameterSymbol)
                    {
                        if (SymbolEqualityComparer.Default.Equals(adapter.Original, parameterSymbol))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(RefactorRule, type.GetLocation(), properties: adapter.Properties, adapter.OriginalMessage, adapter.DestinationMessage));
                        }
                    }
                }
            }
        }
    }
}
