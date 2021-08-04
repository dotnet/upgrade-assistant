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
        private readonly IDiagnosticAnalysisRunner _diagnosticAnalysisRunner;

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

        public SourceUpdaterStep(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, IDiagnosticAnalysisRunner diagnosticAnalysisRunner, ILogger<SourceUpdaterStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _diagnosticAnalysisRunner = diagnosticAnalysisRunner;
            _allAnalyzers = analyzers.OrderBy(a => a.SupportedDiagnostics.First().Id);
            _allCodeFixProviders = codeFixProviders.OrderBy(c => c.FixableDiagnosticIds.First());

            // Add sub-steps for each analyzer that will be run
            SubSteps = new List<UpgradeStep>(_allCodeFixProviders.Select(fixer => new CodeFixerStep(this, GetDiagnosticDescriptorsForCodeFixer(fixer), fixer, _diagnosticAnalysisRunner, logger)));
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

            Diagnostics = await _diagnosticAnalysisRunner.GetDiagnosticsAsync(Project, token).ConfigureAwait(false);

            foreach (var step in SubSteps)
            {
                // Update substep status based on new diagnostic information
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            return Diagnostics.Any() ?
                new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{Diagnostics.Count()} upgrade diagnostics need fixed", BuildBreakRisk.None) :
                new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No upgrade diagnostics found", BuildBreakRisk.None);
        }

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
    }
}
