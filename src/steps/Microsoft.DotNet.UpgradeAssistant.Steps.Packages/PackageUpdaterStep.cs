// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;

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

        private readonly string? _analyzerPackageSource;
        private readonly UpgradeOptions _options;
        private readonly ITargetTFMSelector _tfmSelector;
        private readonly IPackageLoader _packageLoader;
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IPackageReferencesAnalyzer> _packageAnalyzers;

        private PackageAnalysisState? _analysisState;

        public override string Id => typeof(PackageUpdaterStep).FullName!;

        public override string Description => "Update package references to versions compatible with the target framework";

        public override string Title => "Update NuGet packages";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep",

            // Project should be SDK-style before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.TryConvertProjectConverterStep",

            // Project should have correct TFM
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.SetTFMStep",
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        public PackageUpdaterStep(
            UpgradeOptions options,
            IOptions<PackageUpdaterOptions> updaterOptions,
            ITargetTFMSelector tfmSelector,
            IPackageLoader packageLoader,
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

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _analyzerPackageSource = updaterOptions.Value.UpgradeAnalyzersPackageSource;
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
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", context.CurrentProject.Required().FilePath);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Invalid project: {context.CurrentProject.Required().FilePath}", BuildBreakRisk.Unknown);
            }

            if (_analysisState is null || !_analysisState.ChangesRecommended)
            {
                Logger.LogInformation("No package updates needed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }
            else
            {
                if (_analysisState.PackagesToRemove.Count > 0)
                {
                    Logger.LogInformation($"Packages to be removed:\n{string.Join('\n', _analysisState.PackagesToRemove.Distinct())}");
                }

                if (_analysisState.PackagesToAdd.Count > 0)
                {
                    Logger.LogInformation($"Packages to be addded:\n{string.Join('\n', _analysisState.PackagesToAdd.Distinct())}");
                }

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{_analysisState.PackagesToRemove.Distinct().Count()} packages need removed and {_analysisState.PackagesToAdd.Distinct().Count()} packages need added", _analysisState.PossibleBreakingChangeRecommended ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            // TODO : Temporary workaround until the upgrade analyzers are available on NuGet.org
            // Check whether the analyzer package's source is present in NuGet.config and add it if it isn't
            if (_analyzerPackageSource is not null && !_packageLoader.PackageSources.Contains(_analyzerPackageSource))
            {
                // Get or create a local NuGet.config file
                var localNuGetSettings = new Settings(_options.Project.DirectoryName);

                // Add the analyzer package's source to the config file's sources
                localNuGetSettings.AddOrUpdate("packageSources", new SourceItem("migrationAnalyzerSource", _analyzerPackageSource));
                localNuGetSettings.SaveToDisk();
            }

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
                        projectFile.RemovePackages(_analysisState.PackagesToRemove.Distinct());
                        projectFile.AddPackages(_analysisState.PackagesToAdd.Distinct());

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
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", context.CurrentProject.Required().FilePath);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Invalid project: {context.CurrentProject.Required().FilePath}");
            }
        }

        private async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, CancellationToken token)
        {
            _analysisState = await PackageAnalysisState.CreateAsync(context, _tfmSelector, _packageRestorer, token).ConfigureAwait(false);
            var projectRoot = context.CurrentProject;

            if (projectRoot is null)
            {
                Logger.LogError("No project available");
                return false;
            }

            var packageReferences = new PackageCollection(projectRoot.PackageReferences);

            // Iterate through all package references in the project file
            foreach (var analyzer in _packageAnalyzers)
            {
                Logger.LogDebug("Analyzing packages with {AnalyzerName}", analyzer.Name);
                _analysisState = await analyzer.AnalyzeAsync(packageReferences, _analysisState, token).ConfigureAwait(false);
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
