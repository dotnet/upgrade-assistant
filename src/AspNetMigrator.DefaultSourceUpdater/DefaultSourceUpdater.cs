using AspNetMigrator.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class DefaultSourceUpdater : ISourceUpdater
    {
        const string AspNetMigratorAnalyzerPrefix = "AM";

        private ILogger Logger { get; }

        public DefaultSourceUpdater(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> UpdateSourceAsync(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", projectFilePath);
                return false;
            }

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectFilePath);
            var projectId = project.Id;
            Logger.Information("Running ASP.NET Core migration analyzers on {ProjectName}", project.Name);

            var fixCount = 0;
            var diagnosticFixed = false;
            do
            {
                diagnosticFixed = false;
                project = workspace.CurrentSolution.GetProject(projectId);
                var diagnostics = await GetFixableDiagnosticAsync(project).ConfigureAwait(false);

                foreach (var diagnostic in diagnostics)
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree);
                    var updatedSolution = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);

                    if (updatedSolution is null)
                    {
                        Logger.Warning("Failed to fix diagnostic {DiagnosticId} in {FilePath}", diagnostic.Id, doc.FilePath);
                    }
                    else if (updatedSolution != null && workspace.TryApplyChanges(updatedSolution))
                    {
                        Logger.Information("Fixed diagnostic {DiagnosticId} in {FilePath}", diagnostic.Id, doc.FilePath);
                        diagnosticFixed = true;
                        fixCount++;
                        break;
                    }
                    else
                    {
                        Logger.Warning("Failed to apply changes after fixing {DiagnosticId}", diagnostic.Id);
                    }
                }
            } while (diagnosticFixed);

            Logger.Information("Fixed {FixCount} diagnostics", fixCount);
            return true;
        }

        public async Task<Solution> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var provider = AspNetCoreMigrationCodeFixers.AllCodeFixProviders.FirstOrDefault(p => p.FixableDiagnosticIds.Contains(diagnostic.Id));

            if (provider is null)
            {
                Logger.Verbose("No code fix provider found for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            CodeAction fixAction = null;
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context);

            if (fixAction is null)
            {
                Logger.Warning("No code fix found for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(CancellationToken.None)).OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyOperation is null)
            {
                Logger.Warning("Code fix could not be applied for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            return applyOperation.ChangedSolution;
        }

        private async Task<IEnumerable<Diagnostic>> GetFixableDiagnosticAsync(Project project)
        {
            var compilation = (await project.GetCompilationAsync().ConfigureAwait(false))
                .WithAnalyzers(AspNetCoreMigrationAnalyzers.AllAnalyzers, new CompilationWithAnalyzersOptions(new AnalyzerOptions(GetAdditionalFiles()), ProcessAnalyzerException, true, true));
            var diagnostics = (await compilation.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where( d => d.Location.IsInSource && 
                        d.Id.StartsWith(AspNetMigratorAnalyzerPrefix, StringComparison.Ordinal) && 
                        AspNetCoreMigrationCodeFixers.AllCodeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
            Logger.Verbose("Identified {DiagnosticCount} fixable diagnostics in project {ProjectName}", diagnostics.Count(), project.Name);

            return diagnostics;
        }

        private ImmutableArray<AdditionalText> GetAdditionalFiles() => new ImmutableArray<AdditionalText>();

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
