// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    /// <summary>
    /// Base class analyzer for identifying usage of APIs that should be replaced with other APIs.
    /// </summary>
    public abstract class IdentifierUpgradeAnalyzer : DiagnosticAnalyzer
    {
        public const string NewIdentifierKey = "NewIdentifier";

        public abstract IEnumerable<IdentifierMapping> IdentifierMappings { get; }

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeIdentifiers, SyntaxKind.IdentifierName);
        }

        protected abstract Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs);

        private void AnalyzeIdentifiers(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not IdentifierNameSyntax identifier)
            {
                return;
            }

            var name = identifier.Identifier.ValueText;

            // If the node isn't an identifier, bail out
            if (name is null)
            {
                return;
            }

            // If the identifier isn't one of the mapped identifiers, bail out
            var mapping = IdentifierMappings.FirstOrDefault(m => m.SimpleName.Equals(name, StringComparison.Ordinal));
            if (mapping is null)
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't the old identifier, bail out
            if (context.SemanticModel.GetSymbolInfo(identifier).Symbol is INamedTypeSymbol symbol && !symbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldFullName))
            {
                return;
            }

            // If the identified is part of a fully qualified name and the qualified name exactly matches the new full name,
            // then bail out because the code is likely fine and the symbol is just unavailable because of missing references.
            var qualifiedNameSyntax = identifier.GetFullName();
            if (qualifiedNameSyntax.ToString().Equals(mapping.NewFullName))
            {
                return;
            }

            var properties = ImmutableDictionary.Create<string, string?>().Add(NewIdentifierKey, mapping.NewFullName);

            var diagnostic = CreateDiagnostic(identifier.GetLocation(), properties, name, mapping.NewFullName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
