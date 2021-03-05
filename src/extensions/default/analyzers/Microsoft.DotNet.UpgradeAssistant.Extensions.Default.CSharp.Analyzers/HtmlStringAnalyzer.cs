// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.Web)]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class HtmlStringAnalyzer : IdentifierUpgradeAnalyzer
    {
        public const string DiagnosticId = "UA0002";
        private const string Category = "Upgrade";

        public override IEnumerable<IdentifierMapping> IdentifierMappings { get; } = new[]
        {
            new IdentifierMapping("System.Web.HtmlString", "Microsoft.AspNetCore.Html.HtmlString"),
            new IdentifierMapping("System.Web.IHtmlString", "Microsoft.AspNetCore.Html.HtmlString"),
            new IdentifierMapping("System.Web.Mvc.MvcHtmlString", "Microsoft.AspNetCore.Html.HtmlString")
        };

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.HtmlStringTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.HtmlStringMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.HtmlStringDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs) => Diagnostic.Create(Rule, location, properties, messageArgs);
    }
}
