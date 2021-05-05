// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

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

            context.RegisterSymbolAction(AnalyzeSymbols, SymbolKind.NamedType);
        }

        private void AnalyzeSymbols(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            var baseType = namedTypeSymbol.BaseType;

            if (baseType is null)
            {
                return;
            }

            // Find just the named type symbols with names containing lowercase letters.
            if (baseType.ToDisplayString().Equals("System.Web.Http.ApiController", StringComparison.Ordinal))
            {
                // For all such symbols, produce a diagnostic.
                var node = namedTypeSymbol.DeclaringSyntaxReferences[0];

                var generator = GetSyntaxGenerator(context, node).Result;

                var baseAndInterfaceNodes = generator.GetBaseAndInterfaceTypes(node.GetSyntax());

                if (baseAndInterfaceNodes is null || baseAndInterfaceNodes.Count == 0)
                {
                    // The symbol aligns with the ClassStatementSyntax in VB, which does not have a child node
                    // The InerhitsStatementSyntax belongs to the parent
                    baseAndInterfaceNodes = generator.GetBaseAndInterfaceTypes(node.GetSyntax().Parent);
                }

                var diagnostic = Diagnostic.Create(Rule, baseAndInterfaceNodes[0].GetLocation(), namedTypeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static async Task<SyntaxGenerator> GetSyntaxGenerator(SymbolAnalysisContext context, SyntaxReference node)
        {
            // Access to document in DiagnosticAnalyzer? use a workspace
            // https://github.com/dotnet/roslyn/issues/15730
            using var workspace = new AdhocWorkspace();
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());
            workspace.AddSolution(solutionInfo);

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Foo", "Foo", context.Compilation.Language);
            workspace.AddProject(projectInfo);

            var sourceText = node.SyntaxTree.GetText();

            workspace.AddDocument(projectInfo.Id, "Foo", sourceText);

            var slnEditor = new SolutionEditor(workspace.CurrentSolution);

            var theDoc = workspace.CurrentSolution.Projects.First().Documents.First();
            var docEditor = await slnEditor.GetDocumentEditorAsync(theDoc.Id).ConfigureAwait(false);
            return docEditor.Generator;
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
