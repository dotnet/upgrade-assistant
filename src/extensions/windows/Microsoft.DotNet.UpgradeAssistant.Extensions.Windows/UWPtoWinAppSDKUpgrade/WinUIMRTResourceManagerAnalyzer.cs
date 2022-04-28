// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIMRTResourceManagerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA3021";
        private const string Category = "Fix";

        private static readonly LocalizableString Title = "ContentDialog API needs to set XamlRoot";
        private static readonly LocalizableString MessageFormat = "XamlRoot of the Dialog object must be set before making the API call '{0}'";
        private static readonly LocalizableString Description = "Detect content dialog api";

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
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;

            if (!memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;
            }

            var memberName = memberAccessExpression.TryGetInferredMemberName();
            var expression = memberAccessExpression.Expression.ToString();

            // We use the new namespace because by the time this analyzer runs the namespace would have been updated
            if (((expression == "ResourceManager" && UWPToWinUIHelpers.GetAllImportedNamespaces(context).Contains("Microsoft.Windows.ApplicationModel.Resources"))
                || expression == "Microsoft.Windows.ApplicationModel.Resources.ResourceManager")
                && memberName == "Current")
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation(), memberAccessExpression.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
            else if (((expression == "ResourceContext" && UWPToWinUIHelpers.GetAllImportedNamespaces(context).Contains("Microsoft.Windows.ApplicationModel.Resources"))
                || expression == "Microsoft.Windows.ApplicationModel.Resources.ResourceContext")
                && (memberName == "GetForCurrentView" || memberName == "GetForViewIndependentUse"))
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation(), memberAccessExpression.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
