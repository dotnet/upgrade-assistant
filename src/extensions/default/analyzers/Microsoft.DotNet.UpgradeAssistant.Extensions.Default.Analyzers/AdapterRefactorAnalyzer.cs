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
                var adapters = AdapterDescriptor.Parse(context.Compilation);

                if (adapters.Length == 0)
                {
                    return;
                }

                context.RegisterOperationAction(context =>
                {
                    var operation = (IInvocationOperation)context.Operation;

                    foreach (var adapter in adapters)
                    {
                        if (SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, adapter.Original))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(AddMemberRule, operation.Syntax.GetLocation(), properties: adapter.PropertiesWithNewMember(operation.TargetMethod), operation.TargetMethod.Name, adapter.Destination));
                        }
                    }
                }, OperationKind.Invocation);

                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    context.RegisterSyntaxNodeAction(context =>
                    {
                        if (context.Node is CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method)
                        {
                            GeneralizedSyntaxNodeAction(context, adapters, method.ReturnType);
                            GeneralizedParameterAction(context, adapters, method.ParameterList.Parameters, static n => n.Type);
                        }
                        else if (context.Node is CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax field)
                        {
                            GeneralizedSyntaxNodeAction(context, adapters, field.Declaration.Type);
                        }
                        else if (context.Node is CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property)
                        {
                            GeneralizedSyntaxNodeAction(context, adapters, property.Type);
                        }
                    }, CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration, CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration);
                }
                else if (context.Compilation.Language == LanguageNames.VisualBasic)
                {
                    context.RegisterSyntaxNodeAction(context =>
                    {
                        if (context.Node is CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax method)
                        {
                            GeneralizedSyntaxNodeAction(context, adapters, method.AsClause?.Type);
                            GeneralizedParameterAction(context, adapters, method.ParameterList.Parameters, static n => n.AsClause?.Type);
                        }
                        else if (context.Node is CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax property)
                        {
                            if (property.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                            {
                                GeneralizedSyntaxNodeAction(context, adapters, simple.Type);
                            }
                        }
                        else if (context.Node is CodeAnalysis.VisualBasic.Syntax.FieldDeclarationSyntax field)
                        {
                            foreach (var declarator in field.Declarators)
                            {
                                if (declarator.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                                {
                                    GeneralizedSyntaxNodeAction(context, adapters, simple.Type);
                                }
                            }
                        }
                    }, CodeAnalysis.VisualBasic.SyntaxKind.FunctionStatement, CodeAnalysis.VisualBasic.SyntaxKind.SubStatement, CodeAnalysis.VisualBasic.SyntaxKind.FieldDeclaration, CodeAnalysis.VisualBasic.SyntaxKind.PropertyStatement);
                }
            });
        }

        private static void GeneralizedSyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            ImmutableArray<AdapterDescriptor> adapters,
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

            foreach (var adapter in adapters)
            {
                if (SymbolEqualityComparer.Default.Equals(adapter.Original, symbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RefactorRule, syntaxNode.GetLocation(), properties: adapter.Properties, adapter.OriginalMessage, adapter.DestinationMessage));
                }
            }
        }

        private static void GeneralizedParameterAction<TParameter>(
            SyntaxNodeAnalysisContext context,
            ImmutableArray<AdapterDescriptor> adapters,
            SeparatedSyntaxList<TParameter> parameters,
            Func<TParameter, SyntaxNode?> parameterToType)
            where TParameter : SyntaxNode
        {
            var method = context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (method is null)
            {
                return;
            }

            foreach (var adapter in adapters)
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
