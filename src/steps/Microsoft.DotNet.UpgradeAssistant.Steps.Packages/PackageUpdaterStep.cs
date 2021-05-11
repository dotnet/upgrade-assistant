// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Packages;
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
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;

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
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
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

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

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
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FileInfo}", BuildBreakRisk.Unknown);
            }

            if (_analysisState is null || !_analysisState.ChangesRecommended)
            {
                Logger.LogInformation("No package updates needed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }
            else
            {
                LogDetails("References to be removed: {References}", _analysisState.References.Deletions);
                LogDetails("References to be added: {References}", _analysisState.References.Additions);
                LogDetails("Packages to be removed: {Packages}", _analysisState.Packages.Deletions);
                LogDetails("Packages to be added: {Packages}", _analysisState.Packages.Additions);
                LogDetails("Framework references to be added: {FrameworkReference}", _analysisState.FrameworkReferences.Additions);
                LogDetails("Framework references to be removed: {FrameworkReference}", _analysisState.FrameworkReferences.Deletions);

                void LogDetails<T>(string name, ICollection<T> collection)
                {
                    if (collection.Count > 0)
                    {
                        Logger.LogInformation(name, string.Join(Environment.NewLine, collection));
                    }
                }

                return new UpgradeStepInitializeResult(
                    UpgradeStepStatus.Incomplete,
                    $"{_analysisState.References.Deletions.Count} references need removed, {_analysisState.Packages.Deletions.Count} packages need removed, and {_analysisState.Packages.Additions.Count} packages need added", _analysisState.PossibleBreakingChangeRecommended ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
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
                        projectFile.RemoveReferences(_analysisState.References.Deletions);

                        projectFile.RemovePackages(_analysisState.Packages.Deletions);
                        projectFile.AddPackages(_analysisState.Packages.Additions);

                        projectFile.RemoveFrameworkReferences(_analysisState.FrameworkReferences.Deletions);
                        projectFile.AddFrameworkReferences(_analysisState.FrameworkReferences.Additions);

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
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FileInfo}");
            }
        }

        private async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context.CurrentProject is null)
            {
                return false;
            }

            await _packageRestorer.RestorePackagesAsync(context, context.CurrentProject, token).ConfigureAwait(false);
            var nugetReferences = await context.CurrentProject.GetNuGetReferencesAsync(token).ConfigureAwait(false);

            _analysisState = new PackageAnalysisState(context.CurrentProject, nugetReferences);

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
                try
                {
                    await analyzer.AnalyzeAsync(projectRoot, _analysisState, token).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}: {Message}", analyzer.Name, e.Message);
                    return false;
                }
            }

            return true;
        }
    }
}
