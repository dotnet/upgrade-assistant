// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    /*
    Extenders can supply:
        1. An API to alert on
        2. Message
        3. Whether the API needs to resolve to match symbolically or not (maybe call 'support partial syntax match'?)

    Scenarios to support:
    - Base types and interfaces (name syntax)
        - HttpApplication, IHttpModule
    - Attributes (ChildActionOnlyAttribute)
    - Method calls or property invocations (member access syntax)?
        Do I need this or not? There are some things that I'd want to flag on the
        method level but *most* could be on the type level.
        - global.asax.cs registration APIs (RouteCollection.MapMvcAttributeRoutes)
        - Removed HttpContext APIs?
    */

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

        private const string DefaultApiAlertsResourceName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.DefaultApiAlerts.json";
        private IEnumerable<TargetSyntaxMessage> _targetSyntaxes;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public ApiAlertAnalyzer()
        {
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
            };

            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

            using var resourceStream = new StreamReader(typeof(ApiAlertAnalyzer).Assembly.GetManifestResourceStream(DefaultApiAlertsResourceName));
            _targetSyntaxes = JsonSerializer.Deserialize<TargetSyntaxMessage[]>(resourceStream.ReadToEnd(), jsonSerializerOptions)
                ?? throw new InvalidOperationException($"Could not read target syntax messages from resource {DefaultApiAlertsResourceName}");

            // Assemby the list of supported diagnostics
            var supportedDiagnostics = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

            // First, support a generic diagnostic that will be used for API targets specified in additional files
            var genericTitle = new LocalizableResourceString(nameof(Resources.ApiAlertGenericTitle), Resources.ResourceManager, typeof(Resources));
            var genericMessageFormat = new LocalizableResourceString(nameof(Resources.ApiAlertGenericMessageFormat), Resources.ResourceManager, typeof(Resources));
            var genericDesription = new LocalizableResourceString(nameof(Resources.ApiAlertGenericDescription), Resources.ResourceManager, typeof(Resources));
            supportedDiagnostics.Add(new(BaseDiagnosticId, genericTitle, genericMessageFormat, Category, DiagnosticSeverity.Warning, true, genericDesription));

            // Also add all API targets specified specifically in embedded resources
            supportedDiagnostics.AddRange(_targetSyntaxes.Select(s =>
                new DiagnosticDescriptor($"{BaseDiagnosticId}-{s.Id}", $"Replace usage of {string.Join(", ", s.TargetSyntaxes.Select(t => t.FullName))}", s.Message, Category, DiagnosticSeverity.Warning, true, s.Message)));

            SupportedDiagnostics = supportedDiagnostics.ToImmutable();
        }

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
                    // TODO :
                    // TargetSyntaxMessageLoader.LoadMappings(context.Options.AdditionalFiles);
                    Enumerable.Empty<TargetSyntaxMessage>();

                var combinedTargetSyntaxes = _targetSyntaxes.Concat(additionalTargetSyntaxes);

                // Register actions for handling both C# and VB identifiers
                context.RegisterSyntaxNodeAction(context => AnalyzeCSharpIdentifier(context, combinedTargetSyntaxes), CS.SyntaxKind.IdentifierName);
                context.RegisterSyntaxNodeAction(context => AnalyzeVBIdentifier(context, combinedTargetSyntaxes), VB.SyntaxKind.IdentifierName);
            });
        }

        private void AnalyzeCSharpIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TargetSyntaxMessage> targetSyntaxMessages)
        {
            Console.WriteLine();
            var identifier = (CSSyntax.IdentifierNameSyntax)context.Node;
            var symbol = identifier.Parent is CSSyntax.AttributeSyntax
                ? context.SemanticModel.GetTypeInfo(identifier).Type
                : context.SemanticModel.GetSymbolInfo(identifier).Symbol;

            AnalyzeIdentifier(context, symbol, targetSyntaxMessages, identifier.Identifier.ValueText);
        }

        private void AnalyzeVBIdentifier(SyntaxNodeAnalysisContext context, IEnumerable<TargetSyntaxMessage> targetSyntaxMessages)
        {
            var identifier = (VBSyntax.IdentifierNameSyntax)context.Node;
            var symbol = identifier.Parent is VBSyntax.AttributeSyntax
                ? context.SemanticModel.GetTypeInfo(identifier).Type
                : context.SemanticModel.GetSymbolInfo(identifier).Symbol;

            AnalyzeIdentifier(context, symbol, targetSyntaxMessages, identifier.Identifier.ValueText);
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context, ISymbol? symbol, IEnumerable<TargetSyntaxMessage> targetSyntaxMessages, string simpleName)
        {
            // Find target syntax/message mappings that include this node's simple name
            var possibleMatches = targetSyntaxMessages.SelectMany(m => m.TargetSyntaxes.Select(s => (TargetSyntax: s, Mapping: m))).Where(t => t.TargetSyntax.SimpleName.Equals(simpleName, StringComparison.Ordinal));

            if (!possibleMatches.Any())
            {
                return;
            }

            foreach (var match in possibleMatches)
            {
                if (match.TargetSyntax.SyntaxType switch
                {
                    TargetSyntaxType.Member => AnalyzeMember(context.Node, symbol, simpleName, match.TargetSyntax),
                    TargetSyntaxType.Type => AnalyzeType(context.Node, symbol, simpleName, match.TargetSyntax),
                    TargetSyntaxType.Namespace => AnalyzeNamespace(context.Node, symbol, simpleName, match.TargetSyntax),
                    _ => false
                })
                {
                    // Get the diagnostic descriptor correspdoning to the target API message map or, if a specific descriptor doesn't exist for it,
                    // get the default (first) one.
                    var id = $"{BaseDiagnosticId}-{match.Mapping.Id}";
                    var diagnosticDescriptor = SupportedDiagnostics.FirstOrDefault(d => d.Id.Equals(id, StringComparison.Ordinal)) ?? SupportedDiagnostics.First();

                    // Create and report the diagnostic. Note that the fully qualified name's location is used so
                    // that any future code fix provider can directly replace the node without needing to consider its parents.
                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, context.Node.GetQualifiedName().GetLocation(), match.Mapping.Message));
                }
            }
        }

        private bool AnalyzeNamespace(SyntaxNode node, ISymbol? symbol, string simpleName, TargetSyntax targetSyntax)
        {
            // TODO
            return false;
        }

        private bool AnalyzeType(SyntaxNode node, ISymbol? symbol, string simpleName, TargetSyntax targetSyntax)
        {
            // TODO
            return false;
        }

        private bool AnalyzeMember(SyntaxNode node, ISymbol? symbol, string simpleName, TargetSyntax targetSyntax)
        {
            // TODO
            return false;
        }
    }
}
