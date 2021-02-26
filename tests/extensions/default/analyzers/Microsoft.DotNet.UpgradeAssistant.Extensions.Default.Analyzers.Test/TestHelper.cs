// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class TestHelper
    {
        // Path relative from .\bin\debug\net5.0
        // TODO : Make this configurable so the test can pass from other working dirs
        internal const string TestProjectPath = @"assets\TestProject.csproj";

        internal static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers => ImmutableArray.Create<DiagnosticAnalyzer>(
            new UsingSystemWebAnalyzer(),
            new HtmlStringAnalyzer(),
            new ResultTypeAnalyzer(),
            new FilterAnalyzer(),
            new HttpContextCurrentAnalyzer(),
            new HttpContextIsDebuggingEnabledAnalyzer(),
            new HtmlHelperAnalyzer(),
            new UrlHelperAnalyzer(),
            new HelperResultAnalyzer(),
            new AllowHtmlAttributeAnalyzer());

        internal static ImmutableArray<CodeFixProvider> AllCodeFixProviders => ImmutableArray.Create<CodeFixProvider>(
            new UsingSystemWebCodeFixer(),
            new HtmlStringCodeFixer(),
            new ResultTypeCodeFixer(),
            new FilterCodeFixer(),
            new HttpContextCurrentCodeFixer(),
            new HttpContextIsDebuggingEnabledCodeFixer(),
            new HtmlHelperCodeFixer(),
            new UrlHelperCodeFixer(),
            new HelperResultCodeFixer(),
            new AllowHtmlAttributeCodeFixer());

        public static async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(string documentPath, params string[] diagnosticIds)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false);
            return await GetDiagnosticsFromProjectAsync(project, documentPath, diagnosticIds).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<Diagnostic>> GetDiagnosticsFromProjectAsync(Project project, string documentPath, params string[] diagnosticIds)
        {
            var analyzersToUse = AllAnalyzers.Where(a => a.SupportedDiagnostics.Any(d => diagnosticIds.Contains(d.Id, StringComparer.Ordinal)));
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

            if (compilation is null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var compilationWithAnalyzers = compilation
                            .WithAnalyzers(ImmutableArray.Create(analyzersToUse.ToArray()));

            return (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where(d => d.Location.IsInSource && documentPath.Equals(Path.GetFileName(d.Location.GetLineSpan().Path), StringComparison.Ordinal))
                .Where(d => diagnosticIds.Contains(d.Id, StringComparer.Ordinal));
        }

        public static async Task<Document?> GetSourceAsync(string documentPath)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false);
            return project.Documents.FirstOrDefault(d => documentPath.Equals(Path.GetFileName(d.FilePath), StringComparison.Ordinal));
        }

        public static async Task<Document> FixSourceAsync(string documentPath, string diagnosticId)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false);
            var projectId = project.Id;

            var diagnosticFixed = false;
            var solution = workspace.CurrentSolution;
            do
            {
                diagnosticFixed = false;
                project = solution.GetProject(projectId)!;
                var diagnostics = await GetDiagnosticsFromProjectAsync(project, documentPath, diagnosticId).ConfigureAwait(false);

                foreach (var diagnostic in diagnostics)
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree)!;
                    var fixedSolution = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);
                    if (fixedSolution != null)
                    {
                        solution = fixedSolution;
                        diagnosticFixed = true;
                        break;
                    }
                }
            }
            while (diagnosticFixed);

            project = solution.GetProject(projectId)!;
            return project.Documents.First(d => documentPath.Equals(Path.GetFileName(d.FilePath), StringComparison.Ordinal));
        }

        private static async Task<Solution?> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var provider = AllCodeFixProviders.FirstOrDefault(p => p.FixableDiagnosticIds.Contains(diagnostic.Id));

            if (provider is null)
            {
                return null;
            }

            CodeAction? fixAction = null;
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            // fixAction may not be null if the code fixer is applied.
#pragma warning disable CA1508 // Avoid dead conditional code
            if (fixAction is null)
#pragma warning restore CA1508 // Avoid dead conditional code
            {
                return null;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false)).OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyOperation is null)
            {
                return null;
            }

            return applyOperation.ChangedSolution;
        }
    }
}
