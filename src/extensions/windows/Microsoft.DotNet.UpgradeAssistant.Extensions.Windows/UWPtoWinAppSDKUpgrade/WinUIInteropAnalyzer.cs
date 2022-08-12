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
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIInteropAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA312";
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = "Interop APIs should use the window handle";
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

        // Api ID -> KeyValuePair of (UWPType (namespace, type, method) -> WinUIType (namespace, type, method))
        public static readonly Dictionary<string, KeyValuePair<(string UWPNamespace, string UWPType, string UWPMethod), (string WinUINamespace, string WinUIType, string WInUIMethod)?>> UWPToWinUIInteropAPIMap
            = new Dictionary<string, KeyValuePair<(string, string, string), (string, string, string)?>>()
        {
            {
                "1",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.Security.Credentials.UI", "UserConsentVerifier", "RequestVerificationAsync"),
                    ("Windows.Security.Credentials.UI", "UserConsentVerifierInterop", "RequestVerificationForWindowAsync"))
            },
            {
                "2",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.ApplicationModel.DataTransfer.DragDrop.Core", "CoreDragDropManager", "GetForCurrentView"),
                    ("Windows.ApplicationModel.DataTransfer.DragDrop.Core", "DragDropManagerInterop", "GetForWindow"))
            },
            {
                "3",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ViewManagement", "InputPane", "GetForCurrentView"),
                    ("Windows.UI.ViewManagement", "InputPaneInterop", "GetForWindow"))
            },
            {
                "4",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.Graphics.Printing", "PrintManager", "GetForCurrentView"),
                    null)
            },
            {
                "5",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.Media.PlayTo", "PlayToManager", "GetForCurrentView"),
                    ("Windows.Media.PlayTo", "PlayToManagerInterop", "GetForWindow"))
            },
            {
                "6",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.Media.PlayTo", "PlayToManager", "ShowPlayToUI"),
                    ("Windows.Media.PlayTo", "PlayToManagerInterop", "ShowPlayToUIForWindow"))
            },
            {
                "7",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.Input", "RadialControllerConfiguration", "GetForCurrentView"),
                    ("Windows.UI.Input", "RadialControllerConfigurationInterop", "GetForWindow"))
            },
            {
                "8",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.Input.Spatial", "SpatialInteractionManager", "GetForCurrentView"),
                    ("Windows.UI.Input.Spatial", "SpatialInteractionManagerInterop", "GetForWindow"))
            },
            {
                "9",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.Media", "SystemMediaTransportControls", "GetForCurrentView"),
                    ("Windows.Media", "SystemMediaTransportControlsInterop", "GetForWindow"))
            },
            {
                "10",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ViewManagement", "UIViewSettings", "GetForCurrentView"),
                    ("Windows.UI.ViewManagement", "UIViewSettingsInterop", "GetForWindow"))
            },
            {
                "11",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "GetForCurrentView"),
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPaneInterop", "GetForWindow"))
            },
            {
                "12",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "Show"),
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPaneInterop", "ShowAddAccountForWindowAsync"))
            },
            {
                "13",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "ShowAddAccountAsync"),
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPaneInterop", "ShowAddAccountForWindowAsync"))
            },
            {
                "14",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "ShowManageAccountsAsync"),
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPaneInterop", "ShowManageAccountsForWindowAsync"))
            },
            {
                "15",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "ShowAddAccountForUserAsync"),
                    null)
            },
            {
                "16",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Windows.UI.ApplicationSettings", "AccountsSettingsPane", "ShowManageAccountsForUserAsync"),
                    null)
            }
        };

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;
            var firstChildText = node.ChildNodes().First().ToString();
            foreach (var api in UWPToWinUIInteropAPIMap)
            {
                var id = api.Key;
                var (typeNamespace, typeName, methodName) = api.Value.Key;
                if ((firstChildText == $"{typeName}.{methodName}" && node.GetAllImportedNamespaces().Contains(typeNamespace))
                    || firstChildText == $"{typeNamespace}.{typeName}.{methodName}")
                {
                    var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), ImmutableDictionary.Create<string, string?>().Add("apiId", id), node.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
