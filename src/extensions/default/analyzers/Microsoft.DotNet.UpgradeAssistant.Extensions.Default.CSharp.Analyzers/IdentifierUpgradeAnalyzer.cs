// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    /// <summary>
    /// Base class analyzer for identifying usage of APIs that should be replaced with other APIs. This base class
    /// is suitable for creating analyzers that need to identify usage of a type that should be replaced with a
    /// similar type.
    /// </summary>
    public abstract class IdentifierUpgradeAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Key name for the diagnostic property containing the full name of the type
        /// the code fix provider should use to replace the syntax node identified
        /// in the diagnostic.
        /// </summary>
        public const string NewIdentifierKey = "NewIdentifier";

        /// <summary>
        /// Gets an enumerable of <see cref="IdentifierMapping"/> object defining
        /// the identifier names that should be replaced.
        /// </summary>
        public abstract IEnumerable<IdentifierMapping> IdentifierMappings { get; }

        /// <summary>
        /// Initializes the analyzer by registering analysis callback methods.
        /// </summary>
        /// <param name="context">The context to use for initialization.</param>
        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            // Register actions for handling both C# and VB identifiers
            context.RegisterSyntaxNodeAction(AnalyzeCSharpIdentifier, CS.SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeVBIdentifier, VB.SyntaxKind.IdentifierName);
        }

        /// <summary>
        /// Child types should implement this method to create the diagnostic for their specific scenario.
        /// </summary>
        /// <param name="location">The location the diagnostic occurs at.</param>
        /// <param name="properties">Properties (including the name of the new identifier that code fix providers should substitute in) that should be included in the diagnostic.</param>
        /// <param name="messageArgs">Arguments (the simple name of the identifier to be replaced and the full name of the identifier to replace it) to be used in diagnotic messages.</param>
        /// <returns>A diagnostic to be shown to the user.</returns>
        protected abstract Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs);

        private void AnalyzeCSharpIdentifier(SyntaxNodeAnalysisContext context)
        {
            var identifier = (CSSyntax.IdentifierNameSyntax)context.Node;
            AnalyzeIdentifier(context, identifier.Identifier.ValueText);
        }

        private void AnalyzeVBIdentifier(SyntaxNodeAnalysisContext context)
        {
            var identifier = (VBSyntax.IdentifierNameSyntax)context.Node;
            AnalyzeIdentifier(context, identifier.Identifier.ValueText);
        }

        /// <summary>
        /// Analyzes an identifier syntax node to determine if it likely represents any of the types present
        /// in <see cref="IdentifierMappings"/>.
        /// </summary>
        /// <param name="context">The syntax node analysis context including the identifier node to analyze.</param>
        /// <param name="simpleName">The simple name of the identifier being analyzed.</param>
        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, string simpleName)
        {
            // If the identifier isn't one of the mapped identifiers, bail out
            var mapping = IdentifierMappings.FirstOrDefault(m => m.SimpleName.Equals(simpleName, StringComparison.Ordinal));
            if (mapping is null)
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't the old identifier, bail out
            if (context.SemanticModel.GetSymbolInfo(context.Node).Symbol is INamedTypeSymbol symbol
                && !symbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldFullName, StringComparison.Ordinal))
            {
                return;
            }

            // If the identifier is part of a fully qualified name and the qualified name exactly matches the new full name,
            // then bail out because the code is likely fine and the symbol is just unavailable because of missing references.
            var fullyQualifiedNameNode = context.Node.GetQualifiedName();
            if (fullyQualifiedNameNode.ToString().Equals(mapping.NewFullName, StringComparison.Ordinal))
            {
                return;
            }

            // Store the new identifier name that this identifier should be replaced with for use
            // by the code fix provider.
            var properties = ImmutableDictionary.Create<string, string?>().Add(NewIdentifierKey, mapping.NewFullName);

            // Create and report the diagnostic. Note that the fully qualified name's location is used so
            // that the code fix provider can directly replace the node without needing to consider its parents.
            var diagnostic = CreateDiagnostic(fullyQualifiedNameNode.GetLocation(), properties, simpleName, mapping.NewFullName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
