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
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIApiAlertCodeFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            "UA306_A1",
            "UA306_A2",
            "UA306_A3",
            "UA306_A4",
            "UA306_B",
            "UA306_C",
            "UA306_D",
            "UA306_E",
            "UA306_F",
            "UA306_G",
            "UA306_H",
            "UA306_I");

        private readonly ILogger _logger;

        public WinUIApiAlertCodeFixer(ILogger<WinUIApiAlertCodeFixer> logger)
        {
            this._logger = logger;
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                _logger.LogDebug($"[{nameof(WinUIApiAlertCodeFixer)}] skipping code fix registration - root syntax is null");
                return;
            }

            if (!context.Diagnostics.Any())
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindNode(diagnostic.Location.SourceSpan);

            if (declaration is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    string.Empty,
                    c => AddApiAlertComment(context.Document, root, declaration, diagnostic.Id, diagnostic.GetMessage(), diagnostic.Location.GetLineSpan(), c),
                    "Api alerter"),
                diagnostic);
        }

        private async Task<Document> AddApiAlertComment(Document document, SyntaxNode root, SyntaxNode statement, string diagnosticId, string diagnosticMessage,
            FileLinePositionSpan positionSpan, CancellationToken cancellationToken)
        {
            _logger!.LogWarning($"{positionSpan.Path} {diagnosticMessage}");
            var apiAlertComment = @$"/*
                TODO {diagnosticId}: {diagnosticMessage}
            */";
            var comment = await CSharpSyntaxTree.ParseText(apiAlertComment, cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newStatement = statement.WithLeadingTrivia(comment.GetLeadingTrivia());
            return document.WithSyntaxRoot(root.ReplaceNode(statement, newStatement));
        }
    }
}
