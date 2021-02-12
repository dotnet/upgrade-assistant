﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;

namespace Microsoft.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HtmlHelperAnalyzer : DiagnosticAnalyzer
    {
        public const string NewIdentifierKey = "NewIdentifier";
        public const string DiagnosticId = "AM0007";
        private const string Category = "Migration";

        private const string HtmlHelperName = "HtmlHelper";
        private const string SystemWebHtmlHelperName = "System.Web.Mvc.HtmlHelper";
        private const string AspNetCoreHtmlHelperName = "Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper";
        private const string AspNetCoreIHtmlHelperName = "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper";

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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HtmlHelperTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HtmlHelperMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HtmlHelperDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

            // If the identifier isn't HtmlHelper
            if (!name.Equals(HtmlHelperName, StringComparison.Ordinal))
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't System.Web.Mvc.HtmlHelper, then bail out
            if (context.SemanticModel.GetSymbolInfo(identifier).Symbol is INamedTypeSymbol symbol && !symbol.ToString().Equals(SystemWebHtmlHelperName))
            {
                return;
            }

            // Determine the right replacement API. For static usage or constructing an object,
            // use Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper. For arguments, member types,
            // and other uses, use Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.
            var replacementType = GetReplacementType(identifier);

            var properties = ImmutableDictionary.Create<string, string?>().Add(NewIdentifierKey, replacementType);

            var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), properties, name, replacementType);
            context.ReportDiagnostic(diagnostic);
        }

        private static string GetReplacementType(SyntaxNode node)
        {
            var parent = node?.Parent;

            return parent switch
            {
                QualifiedNameSyntax => GetReplacementType(parent),
                ObjectCreationExpressionSyntax => AspNetCoreHtmlHelperName, // Object creation requires HtmlHelper
                MemberAccessExpressionSyntax => AspNetCoreHtmlHelperName, // Member access requires HtmlHelper
                _ => AspNetCoreIHtmlHelperName // Default to IHtmlHelper
            };
        }
    }
}
