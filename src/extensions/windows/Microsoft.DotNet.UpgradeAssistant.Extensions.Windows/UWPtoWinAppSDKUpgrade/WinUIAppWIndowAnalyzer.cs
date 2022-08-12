// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Abstractions;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIAppWindowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdAppWindowType = "UA315_A";
        public const string DiagnosticIdAppWindowMember = "UA315_B";
        public const string DiagnosticIdAppWindowVarType = "UA315_C";

        private const string Category = "Upgrade";

        private const string FixDocumentationUrl = "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing";

        private static readonly LocalizableString Title = "Windows App SDK apps should use Microsoft.UI.Windowing.AppWindow";
        private static readonly LocalizableString MessageFormat = "'{0}' should use Microsoft.UI.Windowing.AppWindow";
        private static readonly LocalizableString Description = "Tries to detect the use of Windows.UI.WindowManagement.AppWindow apis";

        private static readonly DiagnosticDescriptor RuleAppWindowNamespace = new(DiagnosticIdAppWindowType, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleAppWindowVarType = new(DiagnosticIdAppWindowVarType, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleAppWindowMember = new(DiagnosticIdAppWindowMember, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleAppWindowNamespace, RuleAppWindowMember, RuleAppWindowVarType);

        internal static readonly Dictionary<string, ApiUpgrade> TypeApiConversions = new Dictionary<string, ApiUpgrade>()
        {
            {
                "0",
                new ApiUpgrade(
                    new TypeDescription("Windows.UI.WindowManagement", "AppWindow"),
                    new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                    needsManualUpgradation: false,
                    documentationUrl: FixDocumentationUrl)
            },
            {
                "1",
                new ApiUpgrade(
                    new TypeDescription("Windows.UI.ViewManagement", "ApplicationView"),
                    new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                    needsManualUpgradation: true,
                    documentationUrl: FixDocumentationUrl)
            }
        };

        internal static readonly Dictionary<string, ApiUpgrade> MemberApiConversions = new Dictionary<string, ApiUpgrade>()
        {
            {
                "1",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "TryCreateAsync",
                        isStatic: true,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Create",
                        isStatic: true,
                        isAsync: false),
                    needsManualUpgradation: false,
                    documentationUrl: FixDocumentationUrl)
            },
            {
                "2",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "TryShowAsync",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Show",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: false,
                    documentationUrl: FixDocumentationUrl)
            },
            {
                "3",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "CloseAsync",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Destroy",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: false,
                    documentationUrl: FixDocumentationUrl)
            },
            {
                "4",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "GetPlacement",
                        isStatic: false,
                        isAsync: true),
                    new PropertyDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Position",
                        isStatic: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5a",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveAdjacentToCurrentView",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5b",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveAdjacentToWindow",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5c",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveRelativeToCurrentViewContent",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5d",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveRelativeToDisplayRegion",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5e",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveRelativeToWindowContent",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "5f",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "RequestMoveToDisplayRegion",
                        isStatic: false,
                        isAsync: true),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Move",
                        isStatic: false,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: "https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window")
            },
            {
                "6",
                new ApiUpgrade(
                    new MethodDescription(
                        new TypeDescription("Windows.UI.Core", "CoreWindow"),
                        "GetForCurrentThread",
                        isStatic: true,
                        isAsync: false),
                    new MethodDescription(
                        new TypeDescription("Microsoft.UI.Windowing", "AppWindow"),
                        "Create",
                        isStatic: true,
                        isAsync: false),
                    needsManualUpgradation: true,
                    documentationUrl: FixDocumentationUrl)
            },
        };

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var node = (IdentifierNameSyntax)context.Node;
            foreach (var api in TypeApiConversions)
            {
                var fromApi = api.Value.FromApi;
                if (fromApi is not TypeDescription)
                {
                    throw new InvalidOperationException($"Expecting all types in TypeApiConversions dictionary to be of type TypeDescription but found {fromApi.GetType()}");
                }

                var fromType = (TypeDescription)fromApi;
                if (node.Identifier.ValueText.ToString() == fromType.TypeName)
                {
                    if (node.Parent.IsKind(SyntaxKind.QualifiedName))
                    {
                        if (((QualifiedNameSyntax)node.Parent).Left.ToString() == fromType.Namespace)
                        {
                            TryReportDiagnostic(api.Key, node.Parent.GetLocation());
                            return;
                        }

                        continue;
                    }

                    if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        if (node.Parent.ToString() == $"{fromType.Namespace}.{fromType.TypeName}")
                        {
                            TryReportDiagnostic(api.Key, node.Parent.GetLocation());
                            return;
                        }

                        if (!node.Parent.ToString().StartsWith(fromType.TypeName, StringComparison.Ordinal))
                        {
                            return;
                        }
                    }

                    if (node.GetAllImportedNamespaces().Contains(fromType.Namespace))
                    {
                        TryReportDiagnostic(api.Key, node.GetLocation());
                        return;
                    }
                }
            }

            void TryReportDiagnostic(string apiId, Location location)
            {
                // If comment contains the diagnosticId, it has been acknowledged by the code fixer. Consider it fixed.
                if (node.GetLeadingTrivia().ToString().Contains(DiagnosticIdAppWindowType))
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(RuleAppWindowNamespace, location, ImmutableDictionary.Create<string, string?>()
                        .Add("apiId", apiId), node.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;

            var varName = memberAccessExpression.Expression;
            var type = context.SemanticModel.GetTypeInfo(varName);
            var allNamespaces = memberAccessExpression.GetAllImportedNamespaces();
            var memberAccessString = memberAccessExpression.ToString();

            if ((memberAccessString == "AppWindow.Create" && allNamespaces.Contains("Microsoft.UI.Windowing"))
                    || memberAccessString == "Microsoft.UI.Windowing.AppWindow.Create")
            {
                VariableDeclarationSyntax variableDeclarator = memberAccessExpression.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
                if (variableDeclarator is not null)
                {
                    var varIdentifier = variableDeclarator.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.IsVar).FirstOrDefault();
                    if (varIdentifier is not null)
                    {
                        var diagnostic = Diagnostic.Create(RuleAppWindowVarType, memberAccessExpression.GetLocation(), memberAccessExpression.GetText().ToString());
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }

            foreach (var api in MemberApiConversions)
            {
                var id = api.Key;
                if (api.Value.FromApi is not IMemberDescription)
                {
                    throw new InvalidOperationException($"Expecting all types in MemberApiConversions dictionary to be of type ITypeMemberDescription but found {api.Value.FromApi.GetType()}");
                }

                var fromMember = (IMemberDescription)api.Value.FromApi;
                var (typeNamespace, typeName, memberName) = (fromMember.TypeDescription.Namespace, fromMember.TypeDescription.TypeName, fromMember.MemberName);
                if ((memberAccessString == $"{typeName}.{memberName}" && allNamespaces.Contains(typeNamespace))
                    || memberAccessString == $"{typeNamespace}.{typeName}.{memberName}"
                    || (type.Type is not null &&
                        type.Type is not IErrorTypeSymbol &&
                        type.Type.ToString() == $"{typeNamespace}.{typeName}" &&
                        memberAccessExpression.TryGetInferredMemberName() == $"{memberName}"))
                {
                    // If comment contains the diagnosticId, it has been acknowledged by the code fixer. Consider it fixed.
                    if (memberAccessExpression.GetLeadingTrivia().ToString().Contains(DiagnosticIdAppWindowMember))
                    {
                        return;
                    }

                    var diagnostic = Diagnostic.Create(RuleAppWindowMember, memberAccessExpression.GetLocation(), ImmutableDictionary.Create<string, string?>()
                        .Add("apiId", id).Add("varName", varName.ToString()), memberAccessExpression.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
