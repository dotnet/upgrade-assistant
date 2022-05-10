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

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    /// <summary>
    /// Analyzer for identifying usage of types that should be replaced with other types.
    /// Diagnostics are created based on mapping configurations.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TypeUpgradeAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for diagnostics produced by this analyzer.
        /// </summary>
        public const string DiagnosticId = "UA0002";

        /// <summary>
        /// Key name for the diagnostic property containing the full name of the type
        /// the code fix provider should use to replace the syntax node identified
        /// in the diagnostic.
        /// </summary>
        public const string NewIdentifierKey = "NewIdentifier";

        /// <summary>
        /// The diagnsotic category for diagnostics produced by this analyzer.
        /// </summary>
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.TypeUpgradeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.TypeUpgradeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.TypeUpgradeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

            context.RegisterCompilationStartAction(context =>
            {
                // Load analyzer configuration defining the types that should be mapped.
                var mappings = TypeMapLoader.LoadMappings(context.Options.AdditionalFiles);

                // If type maps are present, register syntax node actions to analyze for those types
                if (mappings.Any())
                {
                    // Register actions for handling both C# and VB identifiers
                    context.RegisterSyntaxNodeAction(context => AnalyzeCSharpIdentifier(context, mappings), CS.SyntaxKind.IdentifierName);
                    context.RegisterSyntaxNodeAction(context => AnalyzeVBIdentifier(context, mappings), VB.SyntaxKind.IdentifierName);
                }
            });
        }

        /// <summary>
        /// Creates a type upgrade diagnsotic.
        /// </summary>
        /// <param name="location">The location the diagnostic occurs at.</param>
        /// <param name="properties">Properties (including the name of the new identifier that code fix providers should substitute in) that should be included in the diagnostic.</param>
        /// <param name="messageArgs">Arguments (the simple name of the identifier to be replaced and the full name of the identifier to replace it) to be used in diagnotic messages.</param>
        /// <returns>A diagnostic to be shown to the user.</returns>
        private static Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs)
            => Diagnostic.Create(Rule, location, properties, messageArgs);

        private static void AnalyzeCSharpIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings)
        {
            var identifier = (CSSyntax.IdentifierNameSyntax)context.Node;

            // Replacing attributes is handled in a separate analyzer
            if (identifier.Parent is CSSyntax.AttributeSyntax)
            {
                return;
            }

            AnalyzeIdentifier(context, mappings, identifier.Identifier.ValueText);
        }

        private static void AnalyzeVBIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings)
        {
            var identifier = (VBSyntax.IdentifierNameSyntax)context.Node;

            // Replacing attributes is handled in a separate analyzer
            if (identifier.Parent is VBSyntax.AttributeSyntax)
            {
                return;
            }

            AnalyzeIdentifier(context, mappings, identifier.Identifier.ValueText);
        }

        /// <summary>
        /// Analyzes an identifier syntax node to determine if it likely represents any of the types present
        /// in <see cref="IdentifierMappings"/>.
        /// </summary>
        /// <param name="context">The syntax node analysis context including the identifier node to analyze.</param>
        /// <param name="mappings">Type mappings to use when upgrading types.</param>
        /// <param name="simpleName">The simple name of the identifier being analyzed.</param>
        private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings, string simpleName)
        {
            // If the identifier isn't one of the mapped identifiers, bail out
            var mapping = mappings.FirstOrDefault(m => m.SimpleName?.Equals(simpleName, StringComparison.Ordinal) ?? false);
            if (mapping is null)
            {
                return;
            }

            // This analyzer requires a new type
            if (mapping.NewName is null)
            {
                return;
            }

            var fullyQualifiedNameNode = context.Node.GetQualifiedName();

            // If the identifier is part of a fully qualified name and the qualified name does not match
            // the old name, then bail out because it is not referring to that type
            if (fullyQualifiedNameNode.IsQualifiedName()
                && !mapping.OldName.EndsWith(fullyQualifiedNameNode.ToString(), StringComparison.Ordinal))
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);

            // If symbol information is available, then attempt to use that to determine
            // whether the syntax node corresponds to a type that needs flagged.
            if (symbolInfo.Symbol is ISymbol symbol)
            {
                if (symbol is INamedTypeSymbol typeSymbol)
                {
                    // Bail out if the node corresponds to a type symbol that isn't the old type.
                    if (typeSymbol is not IErrorTypeSymbol
                    && !typeSymbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldName, StringComparison.Ordinal))
                    {
                        return;
                    }
                    else
                    {
                        // This case is intentionally not handled.
                        // A type symbol that is either an error type symbol or that correctly matches the old type name
                        // may warrant reporting a diagnostic.
                    }
                }
                else
                {
                    // If the identifier resolves to a symbol that isn't a type symbol (property symbol, for example),
                    // then bail out because this analyzer should only flag type usage.
                    return;
                }
            }

            // Bail out if the symbol might correspond to a symbol that is the new type.
            if (symbolInfo.CandidateSymbols.OfType<INamedTypeSymbol>().Any(s => s.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.NewName, StringComparison.Ordinal)))
            {
                return;
            }

            // If the symbol cannot be determined, bail out if the syntax is not used in
            // a way that a type symbol would typically be used. This avoids incorrectly
            // reporting diagnostics in cases where an interesting simple name is used
            // for a property name that doesn't resolve to a symbol, for example.
            if (!fullyQualifiedNameNode.IsTypeSyntax())
            {
                return;
            }

            // Store the new identifier name that this identifier should be replaced with for use
            // by the code fix provider.
            var properties = ImmutableDictionary.Create<string, string?>().Add(NewIdentifierKey, mapping.NewName);

            // Create and report the diagnostic. Note that the fully qualified name's location is used so
            // that the code fix provider can directly replace the node without needing to consider its parents.
            var diagnostic = CreateDiagnostic(fullyQualifiedNameNode.GetLocation(), properties, simpleName, mapping.NewName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
