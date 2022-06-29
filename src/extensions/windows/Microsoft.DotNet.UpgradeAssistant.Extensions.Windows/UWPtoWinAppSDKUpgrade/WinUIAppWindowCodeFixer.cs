// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIAppWindowCodeFixer : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType,
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType,
            WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember);

        private const string AppWindowNamespace = "Microsoft.UI.Windowing";
        private const string AppWindowTypeName = "AppWindow";

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null || !context.Diagnostics.Any())
            {
                return;
            }

            var node = root.FindNode(context.Span, findInsideTrivia: false, getInnermostNodeForTie: true);

            var diagnostic = context.Diagnostics.First();
            diagnostic.Properties.TryGetValue("apiId", out var apiId);
            diagnostic.Properties.TryGetValue("varName", out var varName);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindNode(diagnosticSpan)!;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    diagnostic.Id,
                    c => diagnostic.Id == WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType ? FixTypeName(context.Document, root, declaration, apiId!, c)
                    : diagnostic.Id == WinUIAppWindowAnalyzer.DiagnosticIdAppWindowVarType ? FixVarType(context.Document, declaration, c)
                    : FixMember(context.Document, (MemberAccessExpressionSyntax)declaration, apiId!, varName!, c),
                    diagnostic.Id + apiId ?? string.Empty),
                context.Diagnostics);
        }

        private static async Task<Document> FixTypeName(Document document, SyntaxNode oldRoot, SyntaxNode oldNode, string apiId, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var comment = SyntaxFactory.Comment(@$"
                /*
                   TODO {WinUIAppWindowAnalyzer.DiagnosticIdAppWindowType} Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ");

            var apiConversion = WinUIAppWindowAnalyzer.TypeApiConversions[apiId];
            if (apiConversion.ToApi is not TypeDescription)
            {
                throw new InvalidOperationException($"Expecting all types in TypeApiConversions dictionary to be of type TypeDescription but found {apiConversion.ToApi.GetType()}");
            }

            if (apiConversion.NeedsManualUpgradation)
            {
                return document.WithSyntaxRoot(oldRoot.ReplaceNode(oldNode, oldNode.WithLeadingTrivia(comment)));
            }

            var toType = (TypeDescription)apiConversion.ToApi;
            var (newNamespace, newName) = (toType.Namespace, toType.TypeName);
            var node = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(newNamespace), (SimpleNameSyntax)SyntaxFactory.ParseName(newName));

            return document.WithSyntaxRoot(oldRoot.ReplaceNode(oldNode, node.WithLeadingTrivia(comment)));
        }

        private static async Task<Document> FixVarType(Document document, SyntaxNode invocationExpressionSyntax, CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            VariableDeclarationSyntax variableDeclarator = invocationExpressionSyntax.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            if (variableDeclarator is not null)
            {
                var varIdentifier = variableDeclarator.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.IsVar).FirstOrDefault();
                if (varIdentifier is not null)
                {
                    var node = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(AppWindowNamespace), (SimpleNameSyntax)SyntaxFactory.ParseName(AppWindowTypeName));
                    documentEditor.ReplaceNode(varIdentifier, node);
                    return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
                }
            }

            return document;
        }

        private static async Task<Document> FixMember(Document document, MemberAccessExpressionSyntax memberAccessExpression, string apiId, string varName, CancellationToken cancellationToken)
        {
            var apiConversion = WinUIAppWindowAnalyzer.MemberApiConversions[apiId]!;
            if (apiConversion.ToApi is not IMemberDescription)
            {
                throw new InvalidOperationException($"Expecting all types in MemberApiConversions dictionary to be of type ITypeMemberDescription but found {apiConversion.ToApi.GetType()}");
            }

            var fromMember = (IMemberDescription)apiConversion.FromApi;
            var toMember = (IMemberDescription)apiConversion.ToApi;
            var (newTypeNamespace, newTypeName, newMemberName) = (toMember.TypeDescription.Namespace, toMember.TypeDescription.TypeName, toMember.MemberName);

            if (apiConversion.NeedsManualUpgradation)
            {
                var comment = SyntaxFactory.Comment(@$"
                    /* 
                        TODO {WinUIAppWindowAnalyzer.DiagnosticIdAppWindowMember}
                        Use {newTypeNamespace}.{newTypeName}.{newMemberName} instead of {fromMember.MemberName}.
                        Read: {apiConversion.DocumentationUrl}
                    */
                    ");
                var existingRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return document.WithSyntaxRoot(existingRoot!.ReplaceNode(memberAccessExpression, memberAccessExpression.WithLeadingTrivia(comment)));
            }

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var instance = toMember.IsStatic ? $"{newTypeNamespace}.{newTypeName}" : varName;
            if (toMember.ApiType == ApiType.PropertyApi)
            {
                var expr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseExpression(instance), (SimpleNameSyntax)SyntaxFactory.ParseName(newMemberName));

                documentEditor.ReplaceNode(memberAccessExpression, expr.WithLeadingTrivia(memberAccessExpression.GetLeadingTrivia()));
                return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
            }

            var invocationExpression = memberAccessExpression.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var newExpression = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{instance}.{newMemberName}"));
            var newExpressionArgs = newExpression.DescendantNodes().OfType<ArgumentListSyntax>().First();
            var argsToAdd = invocationExpression.DescendantNodes().OfType<ArgumentListSyntax>().First().Arguments.ToArray();
            var newArgs = newExpressionArgs.AddArguments(argsToAdd);
            var newExpressionWithArgs = newExpression.ReplaceNode(newExpressionArgs, newArgs);

            if (fromMember.ApiType == ApiType.MethodApi && toMember.ApiType == ApiType.MethodApi
                && ((MethodDescription)fromMember).IsAsync && !((MethodDescription)toMember).IsAsync)
            {
                var awaitExpression = invocationExpression.Ancestors().OfType<AwaitExpressionSyntax>().FirstOrDefault();
                if (awaitExpression is not null)
                {
                    documentEditor.ReplaceNode(awaitExpression, newExpressionWithArgs.WithLeadingTrivia(invocationExpression.GetLeadingTrivia()));
                    return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
                }
            }

            documentEditor.ReplaceNode(invocationExpression, newExpressionWithArgs.WithLeadingTrivia(invocationExpression.GetLeadingTrivia()));
            return document.WithSyntaxRoot(documentEditor.GetChangedRoot());
        }
    }
}
