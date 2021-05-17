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
        public const string NewIdentifierKey = "NewIdentifier";

        public abstract IEnumerable<IdentifierMapping> IdentifierMappings { get; }

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(AnalyzeCSharpIdentifier, CS.SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeVBIdentifier, VB.SyntaxKind.IdentifierName);
        }

        protected abstract Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs);

        private void AnalyzeCSharpIdentifier(SyntaxNodeAnalysisContext context)
        {
            var identifier = (CSSyntax.IdentifierNameSyntax)context.Node;
            AnalyzeIdentifier(context, identifier.Identifier.ValueText, identifier.GetFullName());
        }

        private void AnalyzeVBIdentifier(SyntaxNodeAnalysisContext context)
        {
            var identifier = (VBSyntax.IdentifierNameSyntax)context.Node;
            AnalyzeIdentifier(context, identifier.Identifier.ValueText, identifier.GetFullName());
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, string simpleName, string fullName)
        {
            // If the node isn't an identifier, bail out
            if (simpleName is null)
            {
                return;
            }

            // If the identifier isn't one of the mapped identifiers, bail out
            var mapping = IdentifierMappings.FirstOrDefault(m => m.SimpleName.Equals(simpleName, StringComparison.Ordinal));
            if (mapping is null)
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't the old identifier, bail out
            // TODO : Add a helper to compare the symbol names more correctly
            if (context.SemanticModel.GetSymbolInfo(context.Node).Symbol is INamedTypeSymbol symbol && !symbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldFullName, StringComparison.Ordinal))
            {
                return;
            }

            // If the identified is part of a fully qualified name and the qualified name exactly matches the new full name,
            // then bail out because the code is likely fine and the symbol is just unavailable because of missing references.
            if (fullName.ToString().Equals(mapping.NewFullName, StringComparison.Ordinal))
            {
                return;
            }

            // Make sure the name syntax node includes the whole name in case it is qualified
            var fullyQualifiedNameNode = GetQualifiedName(context.Node);

            var properties = ImmutableDictionary.Create<string, string?>().Add(NewIdentifierKey, mapping.NewFullName);
            var diagnostic = CreateDiagnostic(fullyQualifiedNameNode.GetLocation(), properties, simpleName, mapping.NewFullName);
            context.ReportDiagnostic(diagnostic);
        }

        private static SyntaxNode GetQualifiedName(SyntaxNode name)
        {
            // If the node is part of a qualified name, we want to get the full qualified name
            while (name.Parent is CSSyntax.NameSyntax || name.Parent is VBSyntax.NameSyntax)
            {
                name = name.Parent;
            }

            // If the node is part of a member access expression (a static member access, for example), then the
            // qualified name will be a member access expression rather than a name syntax.
            if ((name.Parent is CSSyntax.MemberAccessExpressionSyntax csMAE && csMAE.Name.ToString().Equals(name.ToString(), StringComparison.Ordinal))
                || (name.Parent is VBSyntax.MemberAccessExpressionSyntax vbMAE && vbMAE.Name.ToString().Equals(name.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                name = name.Parent;
            }

            return name;
        }
    }
}
