// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Common;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class BinaryFormatterUnsafeDeserializeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0012";
        private const string Category = "Upgrade";

        private const string QualifiedTargetSymbolName = TargetTypeSymbolNamespace + "." + TargetTypeSymbolName;
        private const string TargetTypeSymbolNamespace = "System.Runtime.Serialization.Formatters.Binary";
        private const string TargetTypeSymbolName = "BinaryFormatter";
        private const string TargetMember = "UnsafeDeserialize";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.BinaryFormatterUnsafeDeserializeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSimpleMemberAccessExpression(AnalyzeMemberAccessExpressions);
        }

        private void AnalyzeMemberAccessExpressions(SyntaxNodeAnalysisContext context)
        {
            var isMemberAccessExpresion = GeneralMemberAccessExpression.TryParse(context.Node, out var expression);
            if (!isMemberAccessExpresion || expression is null)
            {
                return;
            }

            // Continue, only if the syntax being evaluated is a method invocation
            if (!expression.IsChildOfInvocationExpression())
            {
                return;
            }

            if (!TargetMember.Equals(expression.GetName(), StringComparison.Ordinal))
            {
                // The thing being invoked isn't a method named "UnsafeDeserialize"
                // bail out
                return;
            }

            // at this point, we have confirmed that the code is invoking a method named "UnsafeDeserialize"
            // next we want to see if this method belongs to System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            var accessedIdentifier = expression.GetAccessedIdentifier();

            var accessedSymbol = context.SemanticModel.GetSymbolInfo(accessedIdentifier).Symbol;
            if (accessedSymbol != null
                && !IsSymbolAVariableOfTheTypeBinaryFormatter(accessedSymbol)
                && !IsSymbolAConstructorInstanceOfBinaryFormatter(accessedSymbol))
            {
                // we found the type that owns this method
                // and we proved it is not "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Checks to see if the symbol is a reference to BinaryFormatter.
        /// </summary>
        /// <param name="theSymbol">any symbol.</param>
        /// <returns>True when (e.g. new BinaryFormatter().UnsafeDeserialize and <paramref name="theSymbol"/> is the symbol representing BinaryFormatter).</returns>
        private static bool IsSymbolAConstructorInstanceOfBinaryFormatter(ISymbol theSymbol)
             => QualifiedTargetSymbolName.Equals(theSymbol.ContainingType.ToString(), StringComparison.Ordinal);

        /// <summary>
        /// Checks to see if the symbol is a reference to BinaryFormatter.
        /// </summary>
        /// <param name="theSymbol">any symbol.</param>
        /// <returns>True when (e.g. formatter1.UnsafeDeserialize and <paramref name="theSymbol"/> is the symbol representing the variable formatter1 of type BinaryFormatter).</returns>
        private static bool IsSymbolAVariableOfTheTypeBinaryFormatter(ISymbol theSymbol)
        {
            var localSymbol = theSymbol as ILocalSymbol;
            if (localSymbol is null)
            {
                return false;
            }

            var typeName = localSymbol.Type?.ToString() ?? string.Empty;

            // using StartsWith because the ToString may produce a nullable type variable ending with '?'
            return typeName.StartsWith(QualifiedTargetSymbolName, StringComparison.Ordinal);
        }
    }
}
