// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
    /// Analyzer for identifying usage of APIs that should be reported to the
    /// user along with messaging about how the API should be (manually) replaced
    /// to complete the upgrade.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ApiAlertAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The base diagnostic ID that diagnostics produced by this analyzer will use as a prefix.
        /// </summary>
        public const string BaseDiagnosticId = "UA0013";

        /// <summary>
        /// The diagnsotic category for diagnostics produced by this analyzer.
        /// </summary>
        private const string Category = "Upgrade";
        private const string AttributeSuffix = "Attribute";
        private const string DefaultApiAlertsResourceName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.DefaultApiAlerts.json";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AttributeUpgradeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AttributeUpgradeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AttributeUpgradeDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor GenericRule = new(BaseDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private Lazy<IEnumerable<TargetSyntaxMessage>> _targetSyntaxes = new Lazy<IEnumerable<TargetSyntaxMessage>>(() =>
        {
            using var resourceStream = new StreamReader(typeof(ApiAlertAnalyzer).Assembly.GetManifestResourceStream(DefaultApiAlertsResourceName));
            return TargetSyntaxMessageLoader.LoadMappings(resourceStream.ReadToEnd())
                ?? throw new InvalidOperationException($"Could not read target syntax messages from resource {DefaultApiAlertsResourceName}");
        });

        // Supported diagnostics include all of the specific diagnostics read from DefaultApiAlerts.json and the generic diagnostic used for additional target syntax messages loaded at runtime.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.CreateRange(_targetSyntaxes.Value
            .Select(t => new DiagnosticDescriptor($"{BaseDiagnosticId}_{t.Id}", $"Replace usage of {string.Join(", ", t.TargetSyntaxes.Select(a => a.FullName))}", t.Message, Category, DiagnosticSeverity.Warning, true, t.Message))
            .Concat(new[] { GenericRule }));

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
                var additionalTargetSyntaxes =
                    TargetSyntaxMessageLoader.LoadMappings(context.Options.AdditionalFiles);

                var combinedTargetSyntaxes = _targetSyntaxes.Value.Concat(additionalTargetSyntaxes);

                // Register actions for handling both C# and VB identifiers
                context.RegisterSyntaxNodeAction(context => AnalyzeIdentifier(context, combinedTargetSyntaxes), CS.SyntaxKind.IdentifierName, CS.SyntaxKind.GenericName);
                context.RegisterSyntaxNodeAction(context => AnalyzeIdentifier(context, combinedTargetSyntaxes), VB.SyntaxKind.IdentifierName, VB.SyntaxKind.GenericName);
            });
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TargetSyntaxMessage> targetSyntaxMessages)
        {
            var simpleName = context.Node switch
            {
                CSSyntax.SimpleNameSyntax csIdentifier => csIdentifier.Identifier.ValueText,
                VBSyntax.SimpleNameSyntax vbIdentifier => vbIdentifier.Identifier.ValueText,
                _ => throw new InvalidOperationException($"Unsupported syntax kind (expected C# or VB identifier name): {context.Node.GetType()}")
            };

            // Find target syntax/message mappings that include this node's simple name
            var fullyQualifiedName = GetFullName(context.Node);

            var stringComparison = context.Node.GetStringComparison();
            var partialMatches = targetSyntaxMessages.SelectMany(m => m.TargetSyntaxes.Select(s => (TargetSyntax: s, Mapping: m)))
                .Where(t => t.TargetSyntax.SyntaxType is TargetSyntaxType.Member

                    // For members, check that the syntax is a method access expression and only match the simple name since
                    // the expression portion of a member access expression may be a local name
                    ? (context.Node.Parent?.IsMemberAccessExpression() ?? false) && t.TargetSyntax.SimpleName.Equals(simpleName, stringComparison)

                    // For types and namespaces, the syntax's entire name needs to match the target
                    : t.TargetSyntax.NameMatcher.MatchesPartiallyQualifiedType(fullyQualifiedName, stringComparison)
                      || t.TargetSyntax.NameMatcher.MatchesPartiallyQualifiedType($"{fullyQualifiedName}{AttributeSuffix}", stringComparison));

            if (!partialMatches.Any())
            {
                return;
            }

            foreach (var match in partialMatches)
            {
                if (match.TargetSyntax.SyntaxType switch
                {
                    TargetSyntaxType.Member => AnalyzeMember(context, match.TargetSyntax),
                    TargetSyntaxType.Type => AnalyzeType(context, match.TargetSyntax),
                    TargetSyntaxType.Namespace => AnalyzeNamespace(context, match.TargetSyntax),
                    _ => false,
                })
                {
                    // Get the diagnostic descriptor correspdoning to the target API message map or, if a specific descriptor doesn't exist for it,
                    // get the default (first) one.
                    var id = $"{BaseDiagnosticId}_{match.Mapping.Id}";
                    var diagnosticDescriptor = SupportedDiagnostics.FirstOrDefault(d => d.Id.Equals(id, StringComparison.Ordinal)) ?? SupportedDiagnostics.First();

                    // Create and report the diagnostic. Note that the fully qualified name's location is used so
                    // that any future code fix provider can directly replace the node without needing to consider its parents.
                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, context.Node.GetQualifiedName().GetLocation(), match.Mapping.Message));
                }
            }
        }

        private static bool AnalyzeNamespace(SyntaxNodeAnalysisContext context, TargetSyntax targetSyntax)
        {
            // If the node is a fully qualified name from the specified namespace, return true
            // This should cover both import statements and fully qualified type names, so no addtional checks
            // for import statements are needed.
            var qualifiedName = GetFullName(context.Node);
            if (qualifiedName.Equals(targetSyntax.FullName, context.Node.GetStringComparison()))
            {
                return true;
            }

            // This method intentionally doesn't check type symbols to see if they're part of the
            // targeted namespace as that diagnostic would likely be too noisy. This will just flag
            // the import statement or fully-qualified use of the namespace name, instead.
            return false;
        }

        private static bool AnalyzeType(SyntaxNodeAnalysisContext context, TargetSyntax targetSyntax)
        {
            // If the node matches the target's fully qualified name, return true.
            var qualifiedName = GetFullName(context.Node);
            if (qualifiedName.Equals(targetSyntax.FullName, context.Node.GetStringComparison()))
            {
                return true;
            }

            // Attempt to get the type symbol (either by getting the type symbol directly,
            // or by getting general symbol info, as required to get the type symbol a ctor
            // corresponds to)
            var symbol = context.SemanticModel.GetTypeInfo(context.Node).Type
                ?? context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

            // If the node's type can be resolved, return true only if it matches
            // the expected full name. If the node resolves to a non-type symbol,
            // return false as this could indicate a local variable or something similar
            // with a name that matches the target.
            if (symbol is not null)
            {
                if (symbol is ITypeSymbol typeSymbol)
                {
                    // This conditional can't be combined with the previous one because
                    // failing this condition should not lead to the else clause.
                    // In other words, if the symbol is a type symbol but is an error type
                    // symbol then don't return here (rather than returning false as would
                    // happen for non-type symbols).
                    if (typeSymbol is not IErrorTypeSymbol)
                    {
                        return targetSyntax.NameMatcher.Matches(typeSymbol);
                    }
                }
                else
                {
                    return false;
                }
            }

            // If the node's full type can't be determined (either by symbol or fully qualified syntax),
            // return true for the partial match only if ambiguous matching is enabled
            return targetSyntax.AlertOnAmbiguousMatch;
        }

        private static bool AnalyzeMember(SyntaxNodeAnalysisContext context, TargetSyntax targetSyntax)
        {
            // If the node matches the target's fully qualified name, return true.
            var qualifiedName = GetFullName(context.Node);
            if (qualifiedName.Equals(targetSyntax.FullName, context.Node.GetStringComparison()))
            {
                return true;
            }

            // If the parent type's symbol is resolvable, return true if
            // it corresponds to the target type
            var typeSyntax = context.Node.Parent?.GetMAEExpressionSyntax();
            if (typeSyntax is not null)
            {
                var symbol = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
                if (symbol is not null && symbol is not IErrorTypeSymbol)
                {
                    return targetSyntax.NameMatcher.Matches(symbol);
                }
            }

            // If the node's full type can't be determined (either by symbol or fully qualified syntax),
            // return true for the partial match only if ambiguous matching is enabled
            return targetSyntax.AlertOnAmbiguousMatch;
        }

        private static string GetFullName(SyntaxNode node)
        {
            var fullName = node.GetQualifiedName().ToString();

            // Ignore generic parameters for now to simplify matching
            var genericParameterIndex = fullName.IndexOfAny(new[] { '<', '(' });
            if (genericParameterIndex > 0)
            {
                fullName = fullName.Substring(0, genericParameterIndex);
            }

            return fullName;
        }
    }
}
