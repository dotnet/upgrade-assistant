// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIApiAlertAnalyzer : ApiAlertAnalyzer
    {
        private const string WinUIApiAlertsResourceName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.WinUIApiAlerts.apitargets";

        public new const string BaseDiagnosticId = "UA3013";

        private const string Category = "Upgrade";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ApiAlertGenericTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ApiAlertGenericMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ApiAlertGenericDescription), Resources.ResourceManager, typeof(Resources));

        protected override DiagnosticDescriptor GenericRule { get; } = new(BaseDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        protected override Lazy<IEnumerable<TargetSyntaxMessage>> TargetSyntaxes => new(() =>
        {
            using var resourceStream = new StreamReader(typeof(WinUIApiAlertAnalyzer).Assembly.GetManifestResourceStream(WinUIApiAlertsResourceName));
            return TargetSyntaxMessageLoader.LoadMappings(resourceStream.ReadToEnd())
                ?? throw new InvalidOperationException($"Could not read target syntax messages from resource {WinUIApiAlertsResourceName}");
        });
    }
}
