// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BinaryFormatterUnsafeDeserializeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0012";
        private const string Category = "Upgrade";

        private const string TargetTypeSymbolNamespace = "System.Runtime.Serialization.Formatters.Binary";
        private const string TargetTypeSymbolName = "BinaryFormatter";
        private const string TargetMember = "UnsafeDeserialize";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalyzeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpressions, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeMemberAccessExpressions(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;

            // If the accessed member isn't named "UnsafeDeserialize" bail out
            if (!TargetMember.Equals(memberAccessExpression.Name.ToString(), StringComparison.Ordinal))
            {
                return;
            }

            // Continue, only if the call is to a method called UnsafeDeserialize
            if (!(memberAccessExpression.Parent is InvocationExpressionSyntax))
            {
                return;
            }

            // Get the identifier accessed
            var accessedIdentifier = memberAccessExpression.Expression switch
            {
                IdentifierNameSyntax i => i,
                ObjectCreationExpressionSyntax o => o.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault(),
                MemberAccessExpressionSyntax m => m.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault(),
                _ => null
            };

            // Return if the accessed identifier wasn't from a simple member access expression, object creation expression, or identifier
            if (accessedIdentifier is null)
            {
                return;
            }

            // If the accessed identifier resolves to a type symbol other than BinaryFormatter, then bail out
            // since it means the user is calling some other similarly named API.
            var accessedSymbol = context.SemanticModel.GetSymbolInfo(accessedIdentifier).Symbol;
            if (accessedSymbol is ILocalSymbol localSymbol)
            {
                // refactored from $"{localSymbol.Type.ContainingNamespace}.{localSymbol.Type.Name}" for performance reasons
                // avoiding the memory pressure from creating an interpolated string due to the volume of times a syntax analyzer is executed
                if (!TargetTypeSymbolNamespace.Equals(localSymbol.Type.ContainingNamespace.ToString(), StringComparison.Ordinal)
                    || !TargetTypeSymbolName.Equals(localSymbol.Type.Name, StringComparison.Ordinal))
                {
                    return;
                }
            }
            else if (accessedSymbol is INamedTypeSymbol symbol)
            {
                var symbolText = symbol.ToDisplayString(NullableFlowState.NotNull);
                if (!symbolText.StartsWith(TargetTypeSymbolNamespace, StringComparison.Ordinal)
                    || !symbolText.EndsWith(TargetTypeSymbolName, StringComparison.Ordinal))
                {
                    return;
                }
            }
            else if (accessedSymbol != null)
            {
                // If the accessed identifier resolves to a symbol other than a ILocalSymbol symbol, bail out
                // since it's not a reference to BinaryFormatter
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
