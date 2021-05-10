// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public class ApiControllerAnalyzerVisualBasic : ApiControllerAnalyzer
    {
        protected override void ReportDiagnostic(SymbolAnalysisContext context, SyntaxNode node)
        {
            if (!this.IsCorrectLanguage(context.Compilation))
            {
                return;
            }

            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Parent is null)
            {
                // ReportDiagnostic is developed on the assumption that the node is a ClassStatementSyntax
                // which does not have child nodes. The InerhitsStatementSyntax belongs to the parent.
                throw new InvalidOperationException($"ReportDiagnostic should not be invoked on a VB SyntaxNode that has no parent.");
            }

            var baseClass = node.Parent.DescendantNodes()
                                .OfType<QualifiedNameSyntax>()
                                .FirstOrDefault() as SyntaxNode;
            if (baseClass is null)
            {
                baseClass = node.Parent.DescendantNodes()
                                .OfType<IdentifierNameSyntax>()
                                .FirstOrDefault();
            }

            var diagnostic = Diagnostic.Create(Rule, baseClass.GetLocation(), baseClass.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
