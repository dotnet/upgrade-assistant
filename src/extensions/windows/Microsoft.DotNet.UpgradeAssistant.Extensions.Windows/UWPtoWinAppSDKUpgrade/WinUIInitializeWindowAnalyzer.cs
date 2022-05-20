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
    public sealed class WinUIInitializeWindowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA310";
        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = "Classes that implement IInitializeWithWindow need to be initialized with Window Handle";
        private static readonly LocalizableString MessageFormat = "The object creation '{0}' should be followed by setting of the window handle";
        private static readonly LocalizableString Description = "Tries to detect the creation of known classes that implement IInitializeWithWindow";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly HashSet<string> InitializeWithWindowTypes = new HashSet<string>
        {
            "FileOpenPicker",
            "FileSavePicker",
            "FolderPicker",
            "PinnedContactManager",
            "PaymentMediator",
            "DevicePicker",
            "GraphicsCapturePicker",
            "CastingDevicePicker",
            "DialDevicePicker",
            "ProvisioningAgent",
            "OnlineIdAuthenticator",
            "StoreContext",
            "FolderLauncherOptions",
            "LauncherOptions",
            "CoreWindowDialog",
            "CoreWindowFlyout",
            "PopupMenu",
            "SecondaryTile",
        };

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            if (node.Type.IsKind(SyntaxKind.IdentifierName))
            {
                if (InitializeWithWindowTypes.Contains(((IdentifierNameSyntax)node.Type).Identifier.ValueText)
                    && !node.Ancestors().OfType<InvocationExpressionSyntax>().Any(expr => expr.Expression.ToString().Contains("InitializeWithWindow")))
                {
                    var diagnostic = Diagnostic.Create(SupportedDiagnostics.First(), node.GetLocation(), node.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
