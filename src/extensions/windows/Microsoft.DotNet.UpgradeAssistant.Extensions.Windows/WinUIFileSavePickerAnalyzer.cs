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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class WinUIFileSavePickerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WinUIFileSavePicker";
        private const string Category = "Fix";

        private static readonly LocalizableString Title = "FileSavePicker API needs to be initialized";
        private static readonly LocalizableString MessageFormat = "Variable '{0}' should be marked const";
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
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;
            if (node.Type.IsKind(SyntaxKind.IdentifierName))
            {
                if (((IdentifierNameSyntax)node.Type).Identifier.ValueText == "FileSavePicker")
                {
                    var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), node.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
