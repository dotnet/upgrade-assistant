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
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AttributeUpgradeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0010";
        private const string Category = "Upgrade";
        private const string AttributeSuffix = "Attribute";

        /// <summary>
        /// Key name for the diagnostic property containing the full name of the
        /// attribute type the code fix provider should use to replace the syntax
        /// node identified in the diagnostic.
        /// </summary>
        public const string NewTypeKey = "NewAttributeType";

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
                if (!context.Compilation.TargetsAspNetCore())
                {
                    return;
                }

                // Load analyzer configuration defining the attribute types that should be mapped.
                var mappings = TypeMapLoader.LoadMappings(context.Options.AdditionalFiles)
                    .Where(m => m.OldName.EndsWith(AttributeSuffix, StringComparison.Ordinal));

                // If attribute type maps are present, register syntax node actions to analyze for those attributes
                if (mappings.Any())
                {
                    // Register actions for handling both C# and VB identifiers
                    context.RegisterSyntaxNodeAction(context => AnalyzeCSharpAttribute(context, mappings), CS.SyntaxKind.Attribute);
                    context.RegisterSyntaxNodeAction(context => AnalyzeVBAttribute(context, mappings), VB.SyntaxKind.Attribute);
                }
            });
        }

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AttributeUpgradeTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AttributeUpgradeMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AttributeUpgradeDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static void AnalyzeCSharpAttribute(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings) =>
            AnalyzeAttribute(context, mappings, ((CSSyntax.AttributeSyntax)context.Node).Name.ToString());

        private static void AnalyzeVBAttribute(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings) =>
            AnalyzeAttribute(context, mappings, ((VBSyntax.AttributeSyntax)context.Node).Name.ToString());

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, IEnumerable<TypeMapping> mappings, string? attributeName)
        {
            if (attributeName is null)
            {
                return;
            }

            // If the attribute name isn't one of the mapped names, bail out
            var stringComparison = context.Node.GetStringComparison();
            var mapping = mappings.FirstOrDefault(m =>
            {
                var matcher = NameMatcher.MatchType(m.OldName);
                return matcher.MatchesPartiallyQualifiedType(attributeName, stringComparison) || matcher.MatchesPartiallyQualifiedType($"{attributeName}{AttributeSuffix}", stringComparison);
            });
            if (mapping is null)
            {
                return;
            }

            // Attempt to get the symbol for the attribute's type
            var attrType = context.SemanticModel.GetTypeInfo(context.Node);

            // Bail out if the node corresponds to a symbol that isn't the old type.
            if (attrType.Type is INamedTypeSymbol typeSymbol
                && typeSymbol is not IErrorTypeSymbol
                && !typeSymbol.ToDisplayString(NullableFlowState.NotNull).Equals(mapping.OldName, StringComparison.Ordinal))
            {
                return;
            }

            // Store the new attribute type that this identifier should be replaced with (or null to remove the attribute)
            // for use by the code fix provider.
            var properties = ImmutableDictionary.Create<string, string?>().Add(NewTypeKey, mapping.NewName);

            // Create and report the diagnostic
            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), properties, mapping.OldName, mapping.NewName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
