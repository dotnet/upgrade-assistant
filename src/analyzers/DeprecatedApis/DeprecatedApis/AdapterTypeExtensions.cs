// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    internal static class AdapterTypeExtensions
    {
        public readonly record struct SyntaxTypeContext(SyntaxNodeAnalysisContext context, ISymbol symbol, SyntaxNode node)
        {
            public void ReportDiagnostic(Diagnostic diagnostic) => context.ReportDiagnostic(diagnostic);
        }

        public static void RegisterTypeAdapterActions(this CompilationStartAnalysisContext context, AdapterContext adapterContext, Action<SyntaxTypeContext> action)
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
                            GeneralizedSyntaxNodeAction(context, action, method.ReturnType);
                            GeneralizedParameterAction(context, action, method.ParameterList.Parameters, static n => n.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax field)
                    {
                        GeneralizedSyntaxNodeAction(context, action, field.Declaration.Type);
                    }
                    else if (context.Node is CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax property)
                    {
                        GeneralizedSyntaxNodeAction(context, action, property.Type);
                    }
                }, CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration, CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration);
            }
            else if (context.Compilation.Language == LanguageNames.VisualBasic)
            {
                context.RegisterSyntaxNodeAction(context =>
                {
                    if (context.Node is CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax method)
                    {
                        GeneralizedSyntaxNodeAction(context, action, method.AsClause?.Type);
                        GeneralizedParameterAction(context, action, method.ParameterList.Parameters, static n => n.AsClause?.Type);
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax property)
                    {
                        if (property.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                        {
                            GeneralizedSyntaxNodeAction(context, action, simple.Type);
                        }
                    }
                    else if (context.Node is CodeAnalysis.VisualBasic.Syntax.FieldDeclarationSyntax field)
                    {
                        foreach (var declarator in field.Declarators)
                        {
                            if (declarator.AsClause is CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax simple)
                            {
                                GeneralizedSyntaxNodeAction(context, action, simple.Type);
                            }
                        }
                    }
                }, CodeAnalysis.VisualBasic.SyntaxKind.FunctionStatement, CodeAnalysis.VisualBasic.SyntaxKind.SubStatement, CodeAnalysis.VisualBasic.SyntaxKind.FieldDeclaration, CodeAnalysis.VisualBasic.SyntaxKind.PropertyStatement);
            }
        }

        private static void GeneralizedSyntaxNodeAction(
            SyntaxNodeAnalysisContext context,
            Action<SyntaxTypeContext> action,
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

            action(new(context, symbol, syntaxNode));
        }

        private static void GeneralizedParameterAction<TParameter>(
            SyntaxNodeAnalysisContext context,
            Action<SyntaxTypeContext> action,
            SeparatedSyntaxList<TParameter> parameters,
            Func<TParameter, SyntaxNode?> parameterToType)
            where TParameter : SyntaxNode
        {
            var method = context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (method is null)
            {
                return;
            }

            foreach (var p in parameters)
            {
                if (parameterToType(p) is SyntaxNode type && context.SemanticModel.GetSymbolInfo(type).Symbol is ISymbol parameterSymbol)
                {
                    action(new(context, parameterSymbol, type));
                }
            }
        }
    }
}
