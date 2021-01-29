using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;

namespace AspNetMigrator.PackageUpdater
{
    /// <summary>
    /// Migration step that updates NuGet package references
    /// to better work after migration. Packages references are
    /// updated if the reference appears to be transitive (with
    /// SDK style projects, only top-level dependencies are necessary
    /// in the project file), if the package version doesn't
    /// target a compatible .NET framework but a newer version does,
    /// or if the package is explicitly mapped to an updated
    /// NuGet package in a mapping configuration file.
    /// </summary>
    public class PackageUpdaterStep : MigrationStep
    {
        private const int MaxAnalysisIterations = 3;

        private readonly string? _analyzerPackageSource;
        private readonly IPackageLoader _packageLoader;
        private readonly IEnumerable<IPackageReferencesAnalyzer> _packageAnalyzers;

        private PackageAnalysisState? _analysisState;

        public PackageUpdaterStep(MigrateOptions options, IOptions<PackageUpdaterOptions> updaterOptions, IPackageLoader packageLoader, IEnumerable<IPackageReferencesAnalyzer> packageAnalyzers,  ILogger<PackageUpdaterStep> logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (updaterOptions is null)
            {
                throw new ArgumentNullException(nameof(updaterOptions));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Title = $"Update NuGet packages";
            Description = $"Update package references in {options.ProjectPath} to versions compatible with the target framework";
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _analyzerPackageSource = updaterOptions.Value.MigrationAnalyzersPackageSource;
            _analysisState = null;
        }

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                if (!await RunPackageAnalyzersAsync(context, token).ConfigureAwait(false))
                {
                    return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Package analysis failed", BuildBreakRisk.Unknown);
                }
            }
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", Options.ProjectPath);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}", BuildBreakRisk.Unknown);
            }

            if (_analysisState is null || (_analysisState.PackagesToRemove.Count == 0 && _analysisState.PackagesToAdd.Count == 0))
            {
                Logger.LogInformation("No package updates needed");
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
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

                return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"{_analysisState.PackagesToRemove.Distinct().Count()} packages need removed and {_analysisState.PackagesToAdd.Distinct().Count()} packages need added", _analysisState.PossibleBreakingChangeRecommended ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
            }
        }

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.Project.Required();

            // TODO : Temporary workaround until the migration analyzers are available on NuGet.org
            // Check whether the analyzer package's source is present in NuGet.config and add it if it isn't
            if (_analyzerPackageSource is not null && !_packageLoader.PackageSources.Contains(_analyzerPackageSource))
            {
                // Get or create a local NuGet.config file
                var localNuGetSettings = new Settings(Path.GetDirectoryName(project.GetRoslynProject().FilePath));

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
                        return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Maximum package analysis and update iterations reached");
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
                            return new MigrationStepApplyResult(MigrationStepStatus.Failed, "Package analysis failed");
                        }
                    }
                }
                while (_analysisState is not null && _analysisState.ChangesRecommended);

                return new MigrationStepApplyResult(MigrationStepStatus.Complete, "Packages updated");
            }
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", Options.ProjectPath);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}");
            }
        }

        private async Task<bool> RunPackageAnalyzersAsync(IMigrationContext context, CancellationToken token)
        {
            _analysisState = null;
            var projectRoot = context.Project;

            if (projectRoot is null)
            {
                Logger.LogError("No project available");
                return false;
            }

            var packageReferences = projectRoot.PackageReferences;

            // Iterate through all package references in the project file
            foreach (var analyzer in _packageAnalyzers)
            {
                Logger.LogDebug("Analyzing packages with {AnalyzerName}", analyzer.Name);
                _analysisState = await analyzer.AnalyzeAsync(context, packageReferences, _analysisState, token).ConfigureAwait(false);
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
