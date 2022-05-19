// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIDataTransferManagerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA311";
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = "Classes that implement IDataTransferManager should use IDataTransferManagerInterop.ShowShareUIForWindow";
        private static readonly LocalizableString MessageFormat = "The object creation '{0}' should be followed by setting of the window handle";
        private static readonly LocalizableString Description = "Tries to detect the creation of known classes that implement IInitializeWithWindow";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.InvocationExpression);
        }

        private static IEnumerable<string> AllNamespaces(SyntaxNodeAnalysisContext context)
        {
            return context.Node.Ancestors().OfType<CompilationUnitSyntax>().First().DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(usingDirective => usingDirective.Name.ToString());
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;
            var namespaces = AllNamespaces(context);
            var firstChildText = node.ChildNodes().First().ToString();
            if ((firstChildText == "DataTransferManager.ShowShareUI" && namespaces.Contains("Windows.ApplicationModel.DataTransfer"))
                || firstChildText == "Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI")
            {
                var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), node.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
