// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AdapterRefactorAnalyzer : DiagnosticAnalyzer
    {
        public const string RefactorDiagnosticId = "UA0014";
        public const string AddMemberDiagnosticId = "UA0014b";
        private const string Category = "Refactor";

        private static readonly LocalizableString RefactorTitle = new LocalizableResourceString(nameof(Resources.AdapterRefactorTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString RefactorMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterRefactorMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString RefactorDescription = new LocalizableResourceString(nameof(Resources.AdapterRefactorDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString AddMemberTitle = new LocalizableResourceString(nameof(Resources.AdapterAddMemberTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddMemberMessageFormat = new LocalizableResourceString(nameof(Resources.AdapterAddMemberMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString AddMemberDescription = new LocalizableResourceString(nameof(Resources.AdapterAddMemberDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor RefactorRule = new(RefactorDiagnosticId, RefactorTitle, RefactorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: RefactorDescription);
        private static readonly DiagnosticDescriptor AddMemberRule = new(AddMemberDiagnosticId, AddMemberTitle, AddMemberMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: AddMemberDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(RefactorRule, AddMemberRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var adapterContext = AdapterContext.Parse(context.Compilation);

                if (!adapterContext.IsAvailable)
                {
                    return;
                }

                // Check to ensure abstractions have members used
                context.RegisterOperationAction(context =>
                {
                    var operation = (IInvocationOperation)context.Operation;

                    foreach (var adapter in adapterContext.Descriptors)
                    {
                        if (SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, adapter.Original))
                        {
                            // TODO: this could be better by matching if it actually binds
                            if (adapter.Destination.GetMembers(operation.TargetMethod.Name).Length == 0)
                            {
                                var properties = adapter.Properties
                                    .WithMissingMethod(operation.TargetMethod);

                                context.ReportDiagnostic(Diagnostic.Create(AddMemberRule, operation.Syntax.GetLocation(), properties: properties, operation.TargetMethod.Name, adapter.Destination));
                            }
                        }
                    }
                }, OperationKind.Invocation);

                // Check that you are using the abstraction
                // TODO: Can we use a symbol/operation action? Need to be more exhaustive on search for types
                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    context.RegisterSyntaxNodeAction(context =>
                    {
                        if (context.Node is CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method)
                        {
                            GeneralizedSyntaxNodeAction(context, adapterContext, method.ReturnType);
                            GeneralizedParameterAction(context, adapterContext, method.ParameterList.Parameters, static n => n.Type);
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
            });
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
