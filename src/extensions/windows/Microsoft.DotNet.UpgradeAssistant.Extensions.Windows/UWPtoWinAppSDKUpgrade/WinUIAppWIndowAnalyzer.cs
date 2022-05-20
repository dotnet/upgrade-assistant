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
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WinUIAppWindowAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdAppWindowNamespace = "UA315_A";
        public const string DiagnosticIdAppWindowMember = "UA315_B";
        public const string DiagnosticIdAppWindowVarType = "UA315_C";

        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = "Windows App SDK apps should use Microsoft.UI.Windowing.AppWindow";
        private static readonly LocalizableString MessageFormat = "'{0}' should use Microsoft.UI.Windowing.AppWindow";
        private static readonly LocalizableString Description = "Tries to detect the use of Windows.UI.WindowManagement.AppWindow apis";

        private static readonly DiagnosticDescriptor RuleAppWindowNamespace = new(DiagnosticIdAppWindowNamespace, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleAppWindowVarType = new(DiagnosticIdAppWindowVarType, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleAppWindowMember = new(DiagnosticIdAppWindowMember, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleAppWindowNamespace, RuleAppWindowMember, RuleAppWindowVarType);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        public static readonly Dictionary<string, KeyValuePair<(string FromNamespace, string FromType), (string ToNamespace, string ToType)>> TypeConversions
            = new Dictionary<string, KeyValuePair<(string, string), (string, string)>>()
        {
            {
                "1",
                new KeyValuePair<(string, string), (string, string)>(
                    ("Windows.UI.WindowManagement", "AppWindow"),
                    ("Microsoft.UI.Windowing", "AppWindow"))
            }
        };

        // Api ID -> KeyValuePair of (FromType (namespace, type, method) -> ToType (namespace, type, method))
        // * means the member is not static and should use the existing variable name while fixing
        public static readonly Dictionary<string, KeyValuePair<(string FromNamespace, string FromType, string FromMethod), (string ToNamespace, string ToType, string ToMethod)?>> MemberConversions
            = new Dictionary<string, KeyValuePair<(string, string, string), (string, string, string)?>>()
        {
            {
                "1",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Microsoft.UI.Windowing", "AppWindow", "TryCreateAsync"),
                    ("Microsoft.UI.Windowing", "AppWindow", "Create"))
            },
            {
                "2",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Microsoft.UI.Windowing", "AppWindow", "TryShowAsync"),
                    ("*", "*", "Show"))
            },
            {
                "3",
                new KeyValuePair<(string, string, string), (string, string, string)?>(
                    ("Microsoft.UI.Windowing", "AppWindow", "CloseAsync"),
                    ("*", "*", "Destroy"))
            }
        };

        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var node = (IdentifierNameSyntax)context.Node;
            foreach (var api in TypeConversions)
            {
                var (fromNamespace, fromType) = api.Value.Key;
                if (node.Identifier.ValueText.ToString() == fromType)
                {
                    if (node.Parent.IsKind(SyntaxKind.QualifiedName))
                    {
                        if (((QualifiedNameSyntax)node.Parent).Left.ToString() == fromNamespace)
                        {
                            ReportDiagnostic(api.Key, node.Parent.GetLocation());
                            return;
                        }

                        continue;
                    }

                    if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        if (node.Parent.ToString() == $"{fromNamespace}.{fromType}")
                        {
                            ReportDiagnostic(api.Key, node.Parent.GetLocation());
                            return;
                        }

                        if (!node.Parent.ToString().StartsWith(fromType, StringComparison.Ordinal))
                        {
                            return;
                        }
                    }

                    if (node.GetAllImportedNamespaces().Contains(fromNamespace))
                    {
                        ReportDiagnostic(api.Key, node.GetLocation());
                        return;
                    }
                }
            }

            void ReportDiagnostic(string apiId, Location location)
            {
                var diagnostic = Diagnostic.Create(RuleAppWindowNamespace, location, ImmutableDictionary.Create<string, string?>()
                        .Add("apiId", apiId), node.GetText().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;

            var memberAccessExpression = node.Expression;
            if (memberAccessExpression is null || !memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;
            }

            var varName = ((MemberAccessExpressionSyntax)memberAccessExpression).Expression;
            var type = context.SemanticModel.GetTypeInfo(varName);
            var allNamespaces = node.GetAllImportedNamespaces();
            var memberAccessString = memberAccessExpression.ToString();

            if ((memberAccessString == "AppWindow.Create" && allNamespaces.Contains("Microsoft.UI.Windowing"))
                    || memberAccessString == "Microsoft.UI.Windowing.AppWindow.Create")
            {
                VariableDeclarationSyntax variableDeclarator = node.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
                if (variableDeclarator is not null)
                {
                    var varIdentifier = variableDeclarator.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.IsVar).FirstOrDefault();
                    if (varIdentifier is not null)
                    {
                        var diagnostic = Diagnostic.Create(RuleAppWindowVarType, node.GetLocation(), node.GetText().ToString());
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }

            foreach (var api in MemberConversions)
            {
                var id = api.Key;
                var (typeNamespace, typeName, methodName) = api.Value.Key;
                if ((memberAccessString == $"{typeName}.{methodName}" && allNamespaces.Contains(typeNamespace))
                    || memberAccessString == $"{typeNamespace}.{typeName}.{methodName}"
                    || (type.Type is not null &&
                        type.Type is not IErrorTypeSymbol &&
                        type.Type.ToString() == $"{typeNamespace}.{typeName}" &&
                        node.Expression?.TryGetInferredMemberName() == $"{methodName}"))
                {
                    var diagnostic = Diagnostic.Create(RuleAppWindowMember, node.GetLocation(), ImmutableDictionary.Create<string, string?>()
                        .Add("apiId", id).Add("varName", varName.ToString()), node.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
