// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WinUIBackButtonAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WinUIBackButton";
        private const string Category = "Fix";

        private static readonly LocalizableString Title = "BackButton does not exist in WinUI";
        private static readonly LocalizableString MessageFormat = "Back button '{0}' should be replaced with WinUI back button";
        private static readonly LocalizableString Description = "Detect UWP back button";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationStatement, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeLocalDeclarationStatement(SyntaxNodeAnalysisContext context)
        {
            var node = (AssignmentExpressionSyntax)context.Node;
            if (!node.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;
            }

            var left = (MemberAccessExpressionSyntax)node.Left;
            if (left.GetLastToken().ValueText != "AppViewBackButtonVisibility")
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), node.GetText().ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
