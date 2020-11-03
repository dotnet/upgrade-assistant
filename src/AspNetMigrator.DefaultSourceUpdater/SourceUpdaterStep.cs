using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspNetMigrator.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace AspNetMigrator.Engine
{
    /// <summary>
    /// Migration step that updates C# source using Roslyn analyzers and code fixers. 
    /// Contains sub-steps for different code fixers.
    /// </summary>
    public class SourceUpdaterStep : MigrationStep
    {
        const string AspNetMigratorAnalyzerPrefix = "AM";
        private MSBuildWorkspace _workspace;
        private ProjectId _projectId;

        internal IEnumerable<Diagnostic> Diagnostics { get; set; }
        internal Project Project => _workspace.CurrentSolution.GetProject(_projectId);


        public SourceUpdaterStep(MigrateOptions options, ILogger logger) : base(options, logger)
        {
            Title = $"Update C# source";
            Description = $"Update source files in {options.ProjectPath} to change ASP.NET references to ASP.NET Core equivalents";

            SubSteps = new List<MigrationStep>(AspNetCoreMigrationCodeFixers.AllCodeFixProviders.Select(fixer => new CodeFixerStep(this, fixer, options, logger)));
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync()
        {
            if (!File.Exists(Options.ProjectPath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", Options.ProjectPath);
                return (MigrationStepStatus.Failed, $"Project file {Options.ProjectPath} not found");
            }

            Logger.Verbose("Opening project {ProjectPath}", Options.ProjectPath);
            _workspace = MSBuildWorkspace.Create();
            var project = await _workspace.OpenProjectAsync(Options.ProjectPath).ConfigureAwait(false);
            _projectId = project.Id;

            await GetDiagnosticsAsync().ConfigureAwait(false);

            foreach (var step in SubSteps)
            {
                // Update substep status based on new diagnostic information
                await step.InitializeAsync().ConfigureAwait(false);
            }

            return Diagnostics.Any() ?
                (MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} migration diagnostics need fixed") :
                (MigrationStepStatus.Complete, null);
        }

        private async Task GetDiagnosticsAsync()
        {
            var project = _workspace.CurrentSolution.GetProject(_projectId);
            Logger.Verbose("Running ASP.NET Core migration analyzers on {ProjectName}", project.Name);

            // Compile with migration analyzers enabled
            var compilation = (await project.GetCompilationAsync().ConfigureAwait(false))
                .WithAnalyzers(AspNetCoreMigrationAnalyzers.AllAnalyzers, new CompilationWithAnalyzersOptions(new AnalyzerOptions(GetAdditionalFiles()), ProcessAnalyzerException, true, true));

            // Find all diagnostics that migration code fixers can address
            Diagnostics = (await compilation.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where(d => d.Location.IsInSource &&
                       d.Id.StartsWith(AspNetMigratorAnalyzerPrefix, StringComparison.Ordinal) &&
                       AspNetCoreMigrationCodeFixers.AllCodeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
            Logger.Verbose("Identified {DiagnosticCount} fixable diagnostics in project {ProjectName}", Diagnostics.Count(), project.Name);
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync() => 
            Task.FromResult(Diagnostics.Any() ?
                (MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} migration diagnostics need fixed") :
                (MigrationStepStatus.Complete, null));

        internal async Task<bool> UpdateSolutionAsync(Solution updatedSolution)
        {
            if (_workspace.TryApplyChanges(updatedSolution))
            {
                Logger.Verbose("Source successfully updated");
                await GetDiagnosticsAsync().ConfigureAwait(false);
                
                // Normally, the migrator will apply steps one at a time
                // at the user's instruction. In the case of parent and child steps, 
                // the parent has any top-level application done after the children.
                // In the case of this update step, the parent (this updater) doesn't
                // need to apply anything. Therefore, automatically apply this updater
                // if all of its children are complete.
                if (!Diagnostics.Any())
                {
                    await ApplyAsync().ConfigureAwait(false);
                }

                return true;
            }
            else
            {
                Logger.Verbose("Failed to apply changes to source");
                return false;
            }
        }

        private ImmutableArray<AdditionalText> GetAdditionalFiles() => new ImmutableArray<AdditionalText>();

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            Logger.Error("Analyzer error while running analyzer {AnalyzerId}: {Exception}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)), exc);
        }
    }
}
