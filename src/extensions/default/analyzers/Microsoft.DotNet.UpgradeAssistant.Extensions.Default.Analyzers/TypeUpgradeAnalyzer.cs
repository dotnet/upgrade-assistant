// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
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

        /// <summary>
        /// The suffix that type map files are expected to use.
        /// </summary>
        private const string TypeMapFileSuffix = ".typemap";

        /// <summary>
        /// An enumerable of <see cref="TypeMapping"/> objects defining
        /// the type names that should be replaced.
        /// </summary>
        private IEnumerable<TypeMapping> _mappings = Enumerable.Empty<TypeMapping>();

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.TypeUpgradeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.TypeUpgradeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.TypeUpgradeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
                _mappings = LoadMappings(context.Options.AdditionalFiles);

                // If type maps are present, register syntax node actions to analyze for those types
                if (_mappings.Any())
                {
                    // Register actions for handling both C# and VB identifiers
                    context.RegisterSyntaxNodeAction(AnalyzeCSharpIdentifier, CS.SyntaxKind.IdentifierName);
                    context.RegisterSyntaxNodeAction(AnalyzeVBIdentifier, VB.SyntaxKind.IdentifierName);
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
            var mapping = _mappings.FirstOrDefault(m => m.SimpleName.Equals(simpleName, StringComparison.Ordinal));
            if (mapping is null)
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't the old identifier, bail out
            if (context.SemanticModel.GetSymbolInfo(context.Node).Symbol is INamedTypeSymbol symbol
                && !symbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldName, StringComparison.Ordinal))
            {
                return;
            }

            // If the identifier is part of a fully qualified name and the qualified name exactly matches the new full name,
            // then bail out because the code is likely fine and the symbol is just unavailable because of missing references.
            var fullyQualifiedNameNode = context.Node.GetQualifiedName();
            if (fullyQualifiedNameNode.ToString().Equals(mapping.NewName, StringComparison.Ordinal))
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

        /// <summary>
        /// Load type mappings from additional files.
        /// </summary>
        /// <returns>Type mappings as defined in *.typemap files in the project's additional files.</returns>
        private static IEnumerable<TypeMapping> LoadMappings(ImmutableArray<AdditionalText> additionalTexts)
        {
            foreach (var file in additionalTexts)
            {
                if (file.Path.EndsWith(TypeMapFileSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in file.GetText()?.Lines ?? Enumerable.Empty<TextLine>())
                    {
                        var components = line.ToString().Trim().Split('\t');
                        if (components.Length == 2)
                        {
                            yield return new TypeMapping(components[0], components[1]);
                        }
                    }
                }
            }
        }
    }
}
