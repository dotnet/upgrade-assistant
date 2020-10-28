using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.Analyzers
{
    public abstract class IdentifierMigrationAnalyzer : DiagnosticAnalyzer
    {
        public const string NewIdentifierKey = "NewIdentifier";
        public abstract IEnumerable<IdentifierMapping> IdentifierMappings { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeIdentifiers, SyntaxKind.IdentifierName);
        }

        protected abstract Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string> properties, params object[] messageArgs);


        private void AnalyzeIdentifiers(SyntaxNodeAnalysisContext context)
        {
            var identifier = context.Node as IdentifierNameSyntax;
            var name = identifier?.Identifier.ValueText;

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
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol;
            if (symbol != null && !symbol.ToString().Equals(mapping.OldFullName))
            {
                return;
            }

            var properties = ImmutableDictionary.Create<string, string>().Add(NewIdentifierKey, mapping.NewFullName);

            var diagnostic = CreateDiagnostic(identifier.GetLocation(), properties, name, mapping.NewFullName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
