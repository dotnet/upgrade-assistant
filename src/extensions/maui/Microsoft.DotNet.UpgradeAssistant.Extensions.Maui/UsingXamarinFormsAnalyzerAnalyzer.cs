// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    [ApplicableComponents(ProjectComponents.Maui)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsingXamarinFormsAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0014";
        private const string Category = "Upgrade";
        private static readonly string[] DisallowedNamespaces = new[] { "Xamarin.Forms" };
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UsingXamarinFormsAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UsingXamarinFormsAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UsingXamarinFormsAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirectives, SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(AnalyzeQualifiedNames, SyntaxKind.QualifiedName);
        }

        private void AnalyzeUsingDirectives(SyntaxNodeAnalysisContext context)
        {
            var usingDirective = (UsingDirectiveSyntax)context.Node;

            var namespaceName = usingDirective.Name?.ToString();

            if (namespaceName is null)
            {
                return;
            }

            if (DisallowedNamespaces.Any(name => namespaceName.Equals(name, StringComparison.Ordinal) || namespaceName.StartsWith($"{name}.", StringComparison.Ordinal)))
            {
                var diagnostic = Diagnostic.Create(Rule, usingDirective.GetLocation(), namespaceName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeQualifiedNames(SyntaxNodeAnalysisContext context)
        {
            var qualifiedNameNode = (QualifiedNameSyntax)context.Node;
            if (qualifiedNameNode is null)
            {
                return;
            }

            var parentNode = qualifiedNameNode.Parent;
            while (parentNode is not null)
            {
                if (parentNode.IsKind(SyntaxKind.UsingDirective) || parentNode.IsKind(SyntaxKind.UsingStatement))
                {
                    return;
                }

                parentNode = parentNode.Parent;
            }

            var qualifiedName = qualifiedNameNode.ToString();
            if (DisallowedNamespaces.Any(name => qualifiedName.Equals(name, StringComparison.Ordinal)))
            {
                var diagnostic = Diagnostic.Create(Rule, qualifiedNameNode.GetLocation(), qualifiedName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
