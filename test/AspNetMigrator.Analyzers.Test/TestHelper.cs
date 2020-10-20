using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace TestProject
{
    public static class TestHelper
    {
        // Path relative from .\bin\debug\net5.0
        // TODO : Make this configurable so the test can pass from other working dirs
        const string TestProjectPath = @"..\..\..\..\TestProject\TestProject.csproj";

        public static async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(string documentPath, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false);
            return await GetDiagnosticsFromProjectAsync(project, documentPath, analyzers).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<Diagnostic>> GetDiagnosticsFromProjectAsync(Project project, string documentPath, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var compilation = (await project.GetCompilationAsync().ConfigureAwait(false))
                            .WithAnalyzers(analyzers);

            return (await compilation.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false)).Where(d => d.Location.IsInSource && documentPath.Equals(Path.GetFileName(d.Location.GetLineSpan().Path), StringComparison.Ordinal));
        }

        public static async Task<Document> GetSourceAsync(string documentPath)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false); ;
            return project.Documents.FirstOrDefault(d => documentPath.Equals(Path.GetFileName(d.FilePath)));
        }

        public static async Task<Document> FixSourceAsync(string documentPath, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(TestProjectPath).ConfigureAwait(false);

            var sourceFixer = new DefaultSourceUpdater(new NullLogger());
            var projectId = project.Id;

            var diagnosticFixed = false;
            var solution = workspace.CurrentSolution;
            do
            {
                diagnosticFixed = false;
                project = solution.GetProject(projectId);
                var diagnostics = await GetDiagnosticsFromProjectAsync(project, documentPath, analyzers).ConfigureAwait(false);

                foreach (var diagnostic in diagnostics)
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree);
                    var fixedSolution = await sourceFixer.TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);
                    if (fixedSolution != null)
                    {
                        solution = fixedSolution;
                        diagnosticFixed = true;
                        break;
                    }
                }
            } while (diagnosticFixed);

            project = solution.GetProject(projectId);
            return project.Documents.FirstOrDefault(d => documentPath.Equals(Path.GetFileName(d.FilePath)));
        }
    }
}
