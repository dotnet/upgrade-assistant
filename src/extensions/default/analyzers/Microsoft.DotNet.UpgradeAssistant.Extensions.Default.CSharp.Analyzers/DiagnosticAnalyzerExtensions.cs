// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    internal static class DiagnosticAnalyzerExtensions
    {
        public static void RegisterMemberAccess(this AnalysisContext context, Action<InvocationAnalysisContext> action)
        {
            var operationKinds = new[]
            {
                OperationKind.Invocation,
                OperationKind.SimpleAssignment,
                OperationKind.VariableDeclaration,
                OperationKind.ObjectCreation,
                OperationKind.FieldInitializer,
                OperationKind.FieldReference,
            };

            context.RegisterOperationAction(ctx =>
            {
                ISymbol? symbol = ctx.Operation switch
                {
                    IInvocationOperation invocation => invocation.TargetMethod,
                    IPropertyReferenceOperation property => property.Property.Type,
                    IObjectCreationOperation creation => creation.Type,
                    ISimpleAssignmentOperation assignment => assignment.Type,
                    IFieldInitializerOperation fieldInitializer => fieldInitializer.Type,
                    IFieldReferenceOperation fieldRef => fieldRef.Type,
                    IVariableDeclarationOperation variableDeclaration => variableDeclaration.Type,
                    _ => null,
                };

                if (symbol is null)
                {
                    return;
                }

                var location = ctx.Operation.Syntax.GetLocation();
                var newCtx = new InvocationAnalysisContext(symbol, location, ctx.Compilation, ctx.Options, ctx.ReportDiagnostic, ctx.CancellationToken);

                action(newCtx);
            }, operationKinds);

            context.RegisterSymbolAction(ctx =>
            {
                var symbol = ctx.Symbol switch
                {
                    IPropertySymbol property => property.Type,
                    IParameterSymbol parameter => parameter.Type,
                    IMethodSymbol method => method.ReturnsVoid ? null : method.ReturnType,
                    IFieldSymbol field => field.Type,
                    _ => null,
                };

                if (symbol is null)
                {
                    return;
                }

                var location = ctx.Symbol.Locations[0];
                var newCtx = new InvocationAnalysisContext(symbol, location, ctx.Compilation, ctx.Options, ctx.ReportDiagnostic, ctx.CancellationToken);

                action(newCtx);
            }, SymbolKind.Property, SymbolKind.Method, SymbolKind.Parameter, SymbolKind.Field);
        }

        public readonly struct InvocationAnalysisContext
        {
            private readonly Action<Diagnostic> _reportDiagnostic;

            public InvocationAnalysisContext(ISymbol symbol, Location location, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                Symbol = symbol;
                Location = location;
                Options = options;
                Compilation = compilation;
                CancellationToken = cancellationToken;

                _reportDiagnostic = reportDiagnostic;
            }

            public Location Location { get; }

            public AnalyzerOptions Options { get; }

            public ISymbol Symbol { get; }

            public Compilation Compilation { get; }

            public CancellationToken CancellationToken { get; }

            public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);
        }
    }
}
