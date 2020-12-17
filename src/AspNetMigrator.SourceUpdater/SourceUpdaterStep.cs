using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.SourceUpdater
{
    /// <summary>
    /// Migration step that updates C# source using Roslyn analyzers and code fixers.
    /// Contains sub-steps for different code fixers.
    /// </summary>
    public class SourceUpdaterStep : MigrationStep
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _allAnalyzers;
        private readonly ImmutableArray<CodeFixProvider> _allCodeFixProviders;

        private Workspace? _workspace;
        private ProjectId? _projectId;

        internal IEnumerable<Diagnostic> Diagnostics { get; set; } = Enumerable.Empty<Diagnostic>();

        internal Project? Project => _workspace?.CurrentSolution.GetProject(_projectId);

        public SourceUpdaterStep(MigrateOptions options, IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, ILogger<SourceUpdaterStep> logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (analyzers is null)
            {
                throw new ArgumentNullException(nameof(analyzers));
            }

            if (codeFixProviders is null)
            {
                throw new ArgumentNullException(nameof(codeFixProviders));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Title = $"Update C# source";
            Description = $"Update source files in {options.ProjectPath} to change ASP.NET references to ASP.NET Core equivalents";

            _allAnalyzers = ImmutableArray.CreateRange(analyzers.OrderBy(a => a.SupportedDiagnostics.First().Id));
            _allCodeFixProviders = ImmutableArray.CreateRange(codeFixProviders.OrderBy(c => c.FixableDiagnosticIds.First()));

            // Add sub-steps for each analyzer that will be run
            SubSteps = new List<MigrationStep>(_allCodeFixProviders.Select(fixer => new CodeFixerStep(this, GetDiagnosticDescriptorsForCodeFixer(fixer), fixer, options, logger)));
        }

        // Gets supported diagnostics from analyzers that are fixable by a given code fixer
        private IEnumerable<DiagnosticDescriptor> GetDiagnosticDescriptorsForCodeFixer(CodeFixProvider fixer) =>
            _allAnalyzers.SelectMany(a => a.SupportedDiagnostics).Where(d => fixer.FixableDiagnosticIds.Contains(d.Id));

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectPath = await context.GetProjectPathAsync(token).ConfigureAwait(false);

            if (!File.Exists(projectPath))
            {
                Logger.LogCritical("Project file {ProjectPath} not found", projectPath);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Project file {projectPath} not found", BuildBreakRisk.Unknown);
            }

            Logger.LogDebug("Opening project {ProjectPath}", projectPath);

            _workspace = await context.GetWorkspaceAsync(token).ConfigureAwait(false);
            _projectId = await context.GetProjectIdAsync(token).ConfigureAwait(false);

            await GetDiagnosticsAsync(context, token).ConfigureAwait(false);

            foreach (var step in SubSteps)
            {
                // Update substep status based on new diagnostic information
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            return Diagnostics.Any() ?
                new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} migration diagnostics need fixed", BuildBreakRisk.None) :
                new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No migration diagnostics found", BuildBreakRisk.None);
        }

        public async Task GetDiagnosticsAsync(IMigrationContext context, CancellationToken token)
        {
            if (_workspace is null)
            {
                Logger.LogWarning("No workspace available.");
                return;
            }

            var project = _workspace.CurrentSolution.GetProject(_projectId);

            if (project is null)
            {
                Logger.LogWarning("No project available.");
                return;
            }

            Logger.LogTrace("Running ASP.NET Core migration analyzers on {ProjectName}", project.Name);

            // Compile with migration analyzers enabled
            var compilation = (await project.GetCompilationAsync(token).ConfigureAwait(false))
                .WithAnalyzers(_allAnalyzers, new CompilationWithAnalyzersOptions(new AnalyzerOptions(GetAdditionalFiles()), ProcessAnalyzerException, true, true));

            // Find all diagnostics that migration code fixers can address
            Diagnostics = (await compilation.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false))
                .Where(d => d.Location.IsInSource &&
                       _allCodeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
            Logger.LogDebug("Identified {DiagnosticCount} fixable diagnostics in project {ProjectName}", Diagnostics.Count(), project.Name);

            // Normally, the migrator will apply steps one at a time
            // at the user's instruction. In the case of parent and child steps,
            // the parent has any top-level application done after the children.
            // In the case of this update step, the parent (this updater) doesn't
            // need to apply anything. Therefore, automatically apply this updater
            // if all of its children are complete. This will avoid the annoying
            // user experience of having to "apply" an empty change for the parent
            // source updater step after all children have applied their changes.
            if (!Diagnostics.Any())
            {
                await ApplyAsync(context, token).ConfigureAwait(false);
            }
        }

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(Diagnostics.Any() ?
                new MigrationStepApplyResult(MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} migration diagnostics need fixed") :
                new MigrationStepApplyResult(MigrationStepStatus.Complete, string.Empty));

        internal bool UpdateSolution(Solution updatedSolution)
        {
            if (_workspace is null)
            {
                Logger.LogWarning("No workspace is available.");
                return false;
            }

            if (_workspace.TryApplyChanges(updatedSolution))
            {
                Logger.LogDebug("Source successfully updated");
                return true;
            }
            else
            {
                Logger.LogDebug("Failed to apply changes to source");
                return false;
            }
        }

        // TODO
        private static ImmutableArray<AdditionalText> GetAdditionalFiles() => default;

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            Logger.LogError("Analyzer error while running analyzer {AnalyzerId}: {Exception}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)), exc);
        }
    }
}
