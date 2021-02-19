// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UrlHelperAnalyzer : DiagnosticAnalyzer
    {
        public const string NewIdentifierKey = "NewIdentifier";
        public const string DiagnosticId = "AM0008";
        private const string Category = "Migration";

        private const string UrlHelperName = "UrlHelper";
        private const string SystemWebUrlHelperName = "System.Web.Mvc.UrlHelper";
        private const string AspNetCoreUrlHelperName = "Microsoft.AspNetCore.Mvc.Routing.UrlHelper";
        private const string AspNetCoreIUrlHelperName = "Microsoft.AspNetCore.Mvc.IUrlHelper";

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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UrlHelperTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UrlHelperMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UrlHelperDescription), Resources.ResourceManager, typeof(Resources));

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
            if (!name.Equals(UrlHelperName, StringComparison.Ordinal))
            {
                return;
            }

            // If the identifier resolves to an actual symbol that isn't System.Web.Mvc.UrlHelper, then bail out
            if (context.SemanticModel.GetSymbolInfo(identifier).Symbol is INamedTypeSymbol symbol && !symbol.ToString().Equals(SystemWebUrlHelperName))
            {
                return;
            }

            // Determine the right replacement API. For constructing an object,
            // use Microsoft.AspNetCore.Mvc.Routing.UrlHelper. For arguments, member types,
            // and other uses, use Microsoft.AspNetCore.Mvc.IUrlHelper.
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
                ObjectCreationExpressionSyntax => AspNetCoreUrlHelperName, // Object creation requires HtmlHelper
                MemberAccessExpressionSyntax => AspNetCoreUrlHelperName, // Member access requires HtmlHelper
                _ => AspNetCoreIUrlHelperName // Default to IHtmlHelper
            };
        }
    }
}
