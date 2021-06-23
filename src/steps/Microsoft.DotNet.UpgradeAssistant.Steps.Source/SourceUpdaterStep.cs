// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    /// <summary>
    /// Upgrade step that updates C# source using Roslyn analyzers and code fixers.
    /// Contains sub-steps for different code fixers.
    /// </summary>
    public class SourceUpdaterStep : UpgradeStep
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _allAnalyzers;
        private readonly IEnumerable<CodeFixProvider> _allCodeFixProviders;
        private readonly ImmutableArray<AdditionalText> _additionalTexts;

        internal IProject? Project { get; private set; }

        internal IEnumerable<Diagnostic> Diagnostics { get; set; } = Enumerable.Empty<Diagnostic>();

        public override string Description => "Update source files to change ASP.NET references to ASP.NET Core equivalents";

        public override string Title => "Update source code";

        public override string Id => WellKnownStepIds.SourceUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing source
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to changing source (since some code fixers will change added templates)
            WellKnownStepIds.TemplateInserterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public SourceUpdaterStep(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, IEnumerable<AdditionalText> additionalTexts, ILogger<SourceUpdaterStep> logger)
            : base(logger)
        {
            if (additionalTexts is null)
            {
                throw new ArgumentNullException(nameof(additionalTexts));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _allAnalyzers = analyzers.OrderBy(a => a.SupportedDiagnostics.First().Id);
            _allCodeFixProviders = codeFixProviders.OrderBy(c => c.FixableDiagnosticIds.First());
            _additionalTexts = ImmutableArray.CreateRange(additionalTexts);

            // Add sub-steps for each analyzer that will be run
            SubSteps = new List<UpgradeStep>(_allCodeFixProviders.Select(fixer => new CodeFixerStep(this, GetDiagnosticDescriptorsForCodeFixer(fixer), fixer, logger)));
        }

        // Gets supported diagnostics from analyzers that are fixable by a given code fixer
        private IEnumerable<DiagnosticDescriptor> GetDiagnosticDescriptorsForCodeFixer(CodeFixProvider fixer) =>
            _allAnalyzers.SelectMany(a => a.SupportedDiagnostics).Where(d => fixer.FixableDiagnosticIds.Contains(d.Id));

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return false;
            }

            foreach (var substep in SubSteps)
            {
                if (await substep.IsApplicableAsync(context, token).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Project = context.CurrentProject.Required();

            var projectPath = Project.FileInfo;

            Logger.LogDebug("Opening project {ProjectPath}", projectPath);

            await GetDiagnosticsAsync(token).ConfigureAwait(false);

            foreach (var step in SubSteps)
            {
                // Update substep status based on new diagnostic information
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            return Diagnostics.Any() ?
                new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{Diagnostics.Count()} upgrade diagnostics need fixed", BuildBreakRisk.None) :
                new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No upgrade diagnostics found", BuildBreakRisk.None);
        }

        public async Task GetDiagnosticsAsync(CancellationToken token)
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

            Logger.LogTrace("Running upgrade analyzers on {ProjectName}", project.Name);

            // Compile with upgrade analyzers enabled
            var applicableAnalyzers = await GetApplicableAnalyzersAsync(_allAnalyzers, Project).ToListAsync(token).ConfigureAwait(false);

            if (!applicableAnalyzers.Any())
            {
                Diagnostics = Enumerable.Empty<Diagnostic>();
            }
            else
            {
                var compilation = await project.GetCompilationAsync(token).ConfigureAwait(false);
                if (compilation is null)
                {
                    Diagnostics = Enumerable.Empty<Diagnostic>();
                }
                else
                {
                    var compilationWithAnalyzer = compilation
                        .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(_additionalTexts), ProcessAnalyzerException, true, true));

                    // Find all diagnostics that upgrade code fixers can address
                    Diagnostics = (await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false))
                        .Where(d => d.Location.IsInSource &&
                               _allCodeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
                    Logger.LogDebug("Identified {DiagnosticCount} fixable diagnostics in project {ProjectName}", Diagnostics.Count(), project.Name);
                }
            }
        }

        private static IAsyncEnumerable<DiagnosticAnalyzer> GetApplicableAnalyzersAsync(IEnumerable<DiagnosticAnalyzer> analyzers, IProject project)
            => analyzers.ToAsyncEnumerable()
                        .WhereAwaitWithCancellation((a, token) => a.GetType().AppliesToProjectAsync(project, token));

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Reload the workspace in case code fixes modified the project file
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            // Apply any necessary cleanup to the project file
            var file = context.CurrentProject.Required().GetFile();
            file.Simplify();
            await file.SaveAsync(token).ConfigureAwait(false);

            if (Diagnostics.Any())
            {
                Logger.LogInformation("Source updates complete with {DiagnosticCount} diagnostics remaining which require manual updates", Diagnostics.Count());
                foreach (var diagnostic in Diagnostics)
                {
                    Logger.LogWarning("Manual updates needed to address: {DiagnosticId}@{DiagnosticLocation}: {DiagnosticMessage}", diagnostic.Id, diagnostic.Location, diagnostic.GetMessage());
                }
            }

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty);
        }

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            Logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
        }
    }
}
