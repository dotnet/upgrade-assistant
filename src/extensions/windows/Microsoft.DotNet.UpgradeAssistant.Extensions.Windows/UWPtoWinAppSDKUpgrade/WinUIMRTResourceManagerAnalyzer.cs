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
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIMRTResourceManagerAnalyzer : DiagnosticAnalyzer
    {
        public const string ResourceManagerAPIDiagnosticId = "UA313";
        public const string ResourceContextAPIDiagnosticId = "UA314";
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = "MRT to MRT core migration";
        private static readonly LocalizableString ResourceManagerMessageFormat = "Creation of ResourceManager '{0}' can be replaced by new ResourceManager()";
        private static readonly LocalizableString ResourceContextMessageFormat = "Creation of Resourcecontext '{0}' can be replaced by ResourceManager.CreateResourceContext";
        private static readonly LocalizableString Description = "Detect presence of MRT ResourceManager and ResourceContext creation";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ResourceManagerAPIRule, ResourceContextAPIRule);

        private static readonly DiagnosticDescriptor ResourceManagerAPIRule = new(ResourceManagerAPIDiagnosticId, Title, ResourceManagerMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor ResourceContextAPIRule = new(ResourceContextAPIDiagnosticId, Title, ResourceContextMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
            if (((expression == "ResourceManager" && memberAccessExpression.GetAllImportedNamespaces().Contains("Microsoft.Windows.ApplicationModel.Resources"))
                || expression == "Microsoft.Windows.ApplicationModel.Resources.ResourceManager")
                && memberName == "Current")
            {
                var diagnostic = Diagnostic.Create(ResourceManagerAPIRule, memberAccessExpression.GetLocation(), memberAccessExpression.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
            else if (((expression == "ResourceContext" && memberAccessExpression.GetAllImportedNamespaces().Contains("Microsoft.Windows.ApplicationModel.Resources"))
                || expression == "Microsoft.Windows.ApplicationModel.Resources.ResourceContext")
                && (memberName == "GetForCurrentView" || memberName == "GetForViewIndependentUse"))
            {
                var diagnostic = Diagnostic.Create(ResourceContextAPIRule, memberAccessExpression.GetLocation(), memberAccessExpression.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
