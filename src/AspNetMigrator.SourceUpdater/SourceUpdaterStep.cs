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

        internal IProject? Project { get; private set; }

        internal IEnumerable<Diagnostic> Diagnostics { get; set; } = Enumerable.Empty<Diagnostic>();

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

            Project = context.Project.Required();

            var projectPath = Project.FilePath;

            Logger.LogDebug("Opening project {ProjectPath}", projectPath);

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
            if (Project is null)
            {
                Logger.LogWarning("No project available.");
                return;
            }

            var project = Project.GetRoslynProject();

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
        }

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(Diagnostics.Any() ?
                new MigrationStepApplyResult(MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} migration diagnostics need fixed") :
                new MigrationStepApplyResult(MigrationStepStatus.Complete, string.Empty));

        // TODO
        private static ImmutableArray<AdditionalText> GetAdditionalFiles() => default;

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            Logger.LogError("Analyzer error while running analyzer {AnalyzerId}: {Exception}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)), exc);
        }
    }
}
