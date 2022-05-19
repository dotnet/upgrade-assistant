// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIApiAlertAnalyzer : ApiAlertAnalyzer
    {
        private const string WinUIApiAlertsResourceName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.WinUIApiAlerts.apitargets";

        public new const string BaseDiagnosticId = "UA306";

        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ApiAlertGenericTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ApiAlertGenericMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ApiAlertGenericDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor ThisAnalyzerDiagnostic = new DiagnosticDescriptor(BaseDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        protected override DiagnosticDescriptor ActiveAnalyzerRule { get; } = ThisAnalyzerDiagnostic;

        protected override bool SkipDiagnostic(SyntaxNodeAnalysisContext context, string detectedDiagnosticId)
        {
            // If the associated comment with the node contains the diagnostic id,
            // it means some code fixer has acknowledged it & added a comment. Do not report it again.
            var nodeStatement = context.Node.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            if (nodeStatement is not null)
            {
                var nodeTrivia = nodeStatement.GetLeadingTrivia();
                if (nodeTrivia.Any(trivia => (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                        && trivia.ToString().Contains(detectedDiagnosticId)))
                {
                    return true;
                }
            }

            return false;
        }

        protected override Lazy<IEnumerable<TargetSyntaxMessage>> TargetSyntaxes => new Lazy<IEnumerable<TargetSyntaxMessage>>(() =>
        {
            using var resourceStream = new StreamReader(typeof(WinUIApiAlertAnalyzer).Assembly.GetManifestResourceStream(WinUIApiAlertsResourceName));
            return TargetSyntaxMessageLoader.LoadMappings(resourceStream.ReadToEnd())
                ?? throw new InvalidOperationException($"Could not read target syntax messages from resource {WinUIApiAlertsResourceName}");
        });
    }
}
