// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    internal static class AdapterTypeExtensions
    {
        public static void RegisterTypeAdapterActions(this CompilationStartAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor, AdapterContext adapterContext)
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
                            GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, method.ReturnType);
                            GeneralizedParameterAction(context, diagnosticDescriptor, adapterContext, method.ParameterList.Parameters, static n => n.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax field)
                    {
                        GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, field.Declaration.Type);
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property)
                    {
                        GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, property.Type);
                    }
                }, CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration, CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration);
            }
            else if (context.Compilation.Language == LanguageNames.VisualBasic)
            {
                context.RegisterSyntaxNodeAction(context =>
                {
                    if (context.Node is CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax method)
                    {
                        GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, method.AsClause?.Type);
                        GeneralizedParameterAction(context, diagnosticDescriptor, adapterContext, method.ParameterList.Parameters, static n => n.AsClause?.Type);
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax property)
                    {
                        if (property.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                        {
                            GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, simple.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.FieldDeclarationSyntax field)
                    {
                        foreach (var declarator in field.Declarators)
                        {
                            if (declarator.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                            {
                                GeneralizedSyntaxNodeAction(context, diagnosticDescriptor, adapterContext, simple.Type);
                            }
                        }
                    }
                }, CodeAnalysis.VisualBasic.SyntaxKind.FunctionStatement, CodeAnalysis.VisualBasic.SyntaxKind.SubStatement, CodeAnalysis.VisualBasic.SyntaxKind.FieldDeclaration, CodeAnalysis.VisualBasic.SyntaxKind.PropertyStatement);
            }
        }

        private static void GeneralizedSyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor diagnosticDescriptor,
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

            foreach (var adapter in adapterContext.TypeDescriptors)
            {
                if (SymbolEqualityComparer.Default.Equals(adapter.Original, symbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, syntaxNode.GetLocation(), properties: adapter.Properties, adapter.OriginalMessage, adapter.DestinationMessage));
                }
            }
        }

        private static void GeneralizedParameterAction<TParameter>(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor diagnosticDescriptor,
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

            foreach (var adapter in adapterContext.TypeDescriptors)
            {
                foreach (var p in parameters)
                {
                    if (parameterToType(p) is SyntaxNode type && context.SemanticModel.GetSymbolInfo(type) is SymbolInfo info && info.Symbol is ISymbol parameterSymbol)
                    {
                        if (SymbolEqualityComparer.Default.Equals(adapter.Original, parameterSymbol))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, type.GetLocation(), properties: adapter.Properties, adapter.OriginalMessage, adapter.DestinationMessage));
                        }
                    }
                }
            }
        }
    }
}
