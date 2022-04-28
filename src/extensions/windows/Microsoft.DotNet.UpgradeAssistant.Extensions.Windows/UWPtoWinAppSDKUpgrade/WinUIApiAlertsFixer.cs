﻿// Licensed to the .NET Foundation under one or more agreements.
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
    public class WinUIApiAlertsfixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            "UA0013_T1",
            "UA0013_T2",
            "UA0013_T3",
            "UA0013_T4",
            "UA0013_U",
            "UA0013_V",
            "UA0013_W",
            "UA0013_X",
            "UA0013_Y",
            "UA0013_Z",
            "UA0013_AA",
            "UA0013_AB");

        private readonly ILogger _logger;

        public WinUIApiAlertsfixer(ILogger<WinUIApiAlertsfixer> logger)
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
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<StatementSyntax>().First();

            if (declaration is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    string.Empty,
                    c => AddApiAlertComment(context.Document, declaration, diagnostic.Id, diagnostic.GetMessage(), diagnostic.Location.GetLineSpan(), c),
                    "Api alerter"),
                diagnostic);
        }

        private async Task<Document> AddApiAlertComment(Document document, StatementSyntax statement, string diagnosticId, string diagnosticMessage,
            FileLinePositionSpan positionSpan, CancellationToken cancellationToken)
        {
            _logger!.LogWarning(positionSpan.Path + " " + diagnosticMessage);
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var apiAlertComment = @$"/*
                TODO {diagnosticId}: {diagnosticMessage}
            */";
            var comment = await CSharpSyntaxTree.ParseText(apiAlertComment, cancellationToken: cancellationToken).GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newStatement = statement.WithLeadingTrivia(comment.GetLeadingTrivia());
            return document.WithSyntaxRoot(oldRoot!.ReplaceNode(statement, newStatement));
        }
    }
}
