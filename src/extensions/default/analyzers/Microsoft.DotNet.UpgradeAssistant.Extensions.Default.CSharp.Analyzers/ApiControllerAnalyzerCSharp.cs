// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiControllerAnalyzerCSharp : ApiControllerAnalyzer
    {
        protected override void ReportDiagnostic(SymbolAnalysisContext context, SyntaxNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!this.IsCorrectLanguage(context.Compilation))
            {
                return;
            }

            var baseClass = node.DescendantNodes()
                                .OfType<QualifiedNameSyntax>()
                                .FirstOrDefault() as SyntaxNode;

            if (baseClass is null)
            {
                baseClass = node.DescendantNodes()
                                .OfType<IdentifierNameSyntax>()
                                .FirstOrDefault();
            }

            var diagnostic = Diagnostic.Create(Rule, baseClass.GetLocation(), baseClass.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
