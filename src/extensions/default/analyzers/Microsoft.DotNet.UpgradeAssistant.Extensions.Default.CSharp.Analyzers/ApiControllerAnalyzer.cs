// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using cs = Microsoft.CodeAnalysis.CSharp;
using vb = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ApiControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA0013";
        private const string Category = "Upgrade";

        public const string BadNamespace = "System.Web.Http";
        public const string BadClassName = "ApiController";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ApiControllerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(AnalyzeNode, cs.SyntaxKind.SimpleBaseType);
            context.RegisterSyntaxNodeAction(AnalyzeNode, vb.SyntaxKind.InheritsStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var namedTypeSymbol = context.ContainingSymbol as INamedTypeSymbol;

            if (namedTypeSymbol != null && namedTypeSymbol.BaseType != null
                && namedTypeSymbol.BaseType.ToDisplayString().Equals($"{BadNamespace}.{BadClassName}", StringComparison.Ordinal))
            {
                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    ReportCSharpSyntax(context);
                }
                else if (context.Compilation.Language == LanguageNames.VisualBasic)
                {
                    ReportVisualBasicSyntax(context);
                }
            }
        }

        private static void ReportCSharpSyntax(SyntaxNodeAnalysisContext context)
        {
            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), context.Node.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Uses syntax analysis to highlight the correct part of the node. At this point
        /// the context.Node looks like 'Inherits System.Web.Http.ApiController' and we
        /// do not want to highlight the VisualBasic.SyntaxKind.InheritsKeyword
        /// </summary>
        /// <param name="context"></param>
        private static void ReportVisualBasicSyntax(SyntaxNodeAnalysisContext context)
        {
            var baseClass = context.Node.DescendantNodes()
                .OfType<vb.Syntax.QualifiedNameSyntax>()
                .FirstOrDefault() as SyntaxNode;

            if (baseClass is null)
            {
                baseClass = context.Node.DescendantNodes()
                .OfType<vb.Syntax.IdentifierNameSyntax>()
                .FirstOrDefault();
            }

            var diagnostic = Diagnostic.Create(Rule, baseClass.GetLocation(), baseClass.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        public static Project AddMetadataReferences(Project project)
        {
            if (project is null)
            {
                return project!;
            }

            // todo - still an open question about how we locate metadatareferences
            const string assemblyFolder = @"C:\deleteMe\";
            var assemblyPath = Path.Combine(assemblyFolder, $"System.Web.Http.dll");

            return project.AddMetadataReference(MetadataReference.CreateFromFile(assemblyPath));
        }
    }
}
