// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    /// <summary>
    /// Upgrade step that updates NuGet package references
    /// to better work after upgrade. Packages references are
    /// updated if the reference appears to be transitive (with
    /// SDK style projects, only top-level dependencies are necessary
    /// in the project file), if the package version doesn't
    /// target a compatible .NET framework but a newer version does,
    /// or if the package is explicitly mapped to an updated
    /// NuGet package in a mapping configuration file.
    /// </summary>
    public class PackageUpdaterStep : UpgradeStep
    {
        private const int MaxAnalysisIterations = 3;

        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IPackageReferencesAnalyzer> _packageAnalyzers;

        private PackageAnalysisState? _analysisState;

        public override string Description => "Update package references to versions compatible with the target framework";

        public override string Title => "Update NuGet Packages";

        public override string Id => WellKnownStepIds.PackageUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            WellKnownStepIds.BackupStepId,

            // Project should be SDK-style before changing package references
            WellKnownStepIds.TryConvertProjectConverterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public PackageUpdaterStep(
            IOptions<PackageUpdaterOptions> updaterOptions,
            IPackageRestorer packageRestorer,
            IEnumerable<IPackageReferencesAnalyzer> packageAnalyzers,
            ILogger<PackageUpdaterStep> logger)
            : base(logger)
        {
            if (updaterOptions is null)
            {
                throw new ArgumentNullException(nameof(updaterOptions));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _analysisState = null;
        }

        protected override bool IsApplicableImpl(IUpgradeContext context) => context?.CurrentProject is not null;

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                if (!await RunPackageAnalyzersAsync(context, token).ConfigureAwait(false))
                {
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Package analysis failed", BuildBreakRisk.Unknown);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FilePath);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FilePath}", BuildBreakRisk.Unknown);
            }

            if (_analysisState is null || !_analysisState.ChangesRecommended)
            {
                Logger.LogInformation("No package updates needed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }
            else
            {
                if (_analysisState.ReferencesToRemove.Count > 0)
                {
                    Logger.LogInformation($"References to be removed:\n{string.Join("\n", _analysisState.ReferencesToRemove.Distinct())}");
                }

                if (_analysisState.PackagesToRemove.Count > 0)
                {
                    Logger.LogInformation($"Packages to be removed:\n{string.Join("\n", _analysisState.PackagesToRemove.Distinct())}");
                }

                if (_analysisState.FrameworkReferencesToRemove.Count > 0)
                {
                    Logger.LogInformation($"Framework references to be removed:\n{string.Join("\n", _analysisState.FrameworkReferencesToRemove.Distinct())}");
                }

                if (_analysisState.PackagesToAdd.Count > 0)
                {
                    Logger.LogInformation($"Packages to be addded:\n{string.Join("\n", _analysisState.PackagesToAdd.Distinct())}");
                }

                if (_analysisState.FrameworkReferencesToAdd.Count > 0)
                {
                    Logger.LogInformation($"Framework references to be addded:\n{string.Join("\n", _analysisState.FrameworkReferencesToAdd.Distinct())}");
                }

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{_analysisState.ReferencesToRemove.Distinct().Count()} references need removed, {_analysisState.PackagesToRemove.Distinct().Count()} packages need removed, and {_analysisState.PackagesToAdd.Distinct().Count()} packages need added", _analysisState.PossibleBreakingChangeRecommended ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            try
            {
                var projectFile = project.GetFile();

                var count = 0;
                do
                {
                    if (count >= MaxAnalysisIterations)
                    {
                        Logger.LogError("Maximum package analysis and update iterations reached. Review NuGet dependencies manually");
                        return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Maximum package analysis and update iterations reached");
                    }

                    if (_analysisState is not null)
                    {
                        projectFile.RemoveReferences(_analysisState.ReferencesToRemove.Distinct());
                        projectFile.RemovePackages(_analysisState.PackagesToRemove.Distinct());
                        projectFile.RemoveFrameworkReferences(_analysisState.FrameworkReferencesToRemove.Distinct());
                        projectFile.AddPackages(_analysisState.PackagesToAdd.Distinct());
                        projectFile.AddFrameworkReferences(_analysisState.FrameworkReferencesToAdd.Distinct());

                        await projectFile.SaveAsync(token).ConfigureAwait(false);
                        count++;

                        Logger.LogDebug("Re-running analysis to check whether additional changes are needed");
                        if (!await RunPackageAnalyzersAsync(context, token).ConfigureAwait(false))
                        {
                            return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Package analysis failed");
                        }
                    }
                }
                while (_analysisState is not null && _analysisState.ChangesRecommended);

                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Packages updated");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FilePath);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FilePath}");
            }
        }

        private async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, CancellationToken token)
        {
            _analysisState = await PackageAnalysisState.CreateAsync(context, _packageRestorer, token).ConfigureAwait(false);
            var projectRoot = context.CurrentProject;

            if (projectRoot is null)
            {
                Logger.LogError("No project available");
                return false;
            }

            // Iterate through all package references in the project file
            foreach (var analyzer in _packageAnalyzers)
            {
                Logger.LogDebug("Analyzing packages with {AnalyzerName}", analyzer.Name);
                _analysisState = await analyzer.AnalyzeAsync(projectRoot, _analysisState, token).ConfigureAwait(false);
                if (_analysisState.Failed)
                {
                    Logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}", analyzer.Name);
                    return false;
                }
            }

            return true;
        }
    }
}
