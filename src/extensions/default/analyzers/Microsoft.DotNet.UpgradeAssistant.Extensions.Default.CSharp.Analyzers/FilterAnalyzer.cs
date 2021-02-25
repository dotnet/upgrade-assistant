// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FilterAnalyzer : IdentifierUpgradeAnalyzer
    {
        public const string DiagnosticId = "UA0004";
        private const string Category = "Upgrade";

        public override IEnumerable<IdentifierMapping> IdentifierMappings { get; } = new[]
        {
            new IdentifierMapping("System.Web.Mvc.ResultExecutingContext", "Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext"),
            new IdentifierMapping("System.Web.Mvc.ResultExecutedContext", "Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext"),
            new IdentifierMapping("System.Web.Mvc.IResultFilter", "Microsoft.AspNetCore.Mvc.Filters.IResultFilter"),
            new IdentifierMapping("System.Web.Mvc.ActionFilterAttribute", "Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute"),
            new IdentifierMapping("System.Web.Mvc.ActionExecutingContext", "Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext"),
            new IdentifierMapping("System.Web.Mvc.ActionExecutedContext", "Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext"),
            new IdentifierMapping("System.Web.Mvc.IActionFilter", "Microsoft.AspNetCore.Mvc.Filters.IActionFilter")
        };

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.FilterTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.FilterMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.FilterDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override Diagnostic CreateDiagnostic(Location location, ImmutableDictionary<string, string?> properties, params object[] messageArgs) => Diagnostic.Create(Rule, location, properties, messageArgs);
    }
}
