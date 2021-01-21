using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;

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
        private const string AnalyzerPackageName = "AspNetMigrator.Analyzers";
        private const string PackageMapExtension = "*.json";

        private readonly string? _analyzerPackageSource;
        private readonly string? _analyzerPackageVersion;
        private readonly IPackageLoader _packageLoader;
        private readonly IPackageRestorer _packageRestorer;
        private readonly string _packageMapSearchPath;
        private readonly bool _logRestoreOutput;
        private readonly NuGetFramework _targetFramework;
        private List<NuGetReference> _packagesToRemove;
        private List<NuGetReference> _packagesToAdd;

        public PackageUpdaterStep(MigrateOptions options, IPackageLoader packageLoader, IPackageRestorer packageRestorer, IOptions<PackageUpdaterStepOptions> updaterOptions, ILogger<PackageUpdaterStep> logger)
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
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageMapSearchPath = Path.IsPathFullyQualified(updaterOptions.Value.PackageMapPath ?? string.Empty)
                ? updaterOptions.Value.PackageMapPath!
                : Path.Combine(AppContext.BaseDirectory, updaterOptions.Value.PackageMapPath ?? string.Empty);
            _analyzerPackageSource = updaterOptions.Value.MigrationAnalyzersPackageSource;
            _analyzerPackageVersion = updaterOptions.Value.MigrationAnalyzersPackageVersion;
            _logRestoreOutput = updaterOptions.Value.LogRestoreOutput;
            _targetFramework = NuGetFramework.Parse(options.TargetFramework);
            _packagesToRemove = new List<NuGetReference>();
            _packagesToAdd = new List<NuGetReference>();
        }

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var possibleBreakingChanges = false;
            _packagesToRemove = new List<NuGetReference>();
            _packagesToAdd = new List<NuGetReference>();

            // Read package maps
            var packageMaps = await LoadPackageMapsAsync(token).ConfigureAwait(false);

            // Restore packages (to produce lockfile)
            var restoreOutput = await _packageRestorer.RestorePackagesAsync(_logRestoreOutput, context, token).ConfigureAwait(false);
            if (restoreOutput.LockFilePath is null)
            {
                var project = context.Project.Required();
                var path = project.FilePath;
                Logger.LogCritical("Unable to restore packages for project {ProjectPath}", path);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Unable to restore packages for project {path}", BuildBreakRisk.Unknown);
            }

            try
            {
                // Iterate through all package references in the project file
                // TODO : Parallelize
                var projectRoot = context.Project;

                if (projectRoot is null)
                {
                    return new MigrationStepInitializeResult(MigrationStepStatus.Failed, "No project available", BuildBreakRisk.None);
                }

                var packageReferences = projectRoot.PackageReferences;

                foreach (var packageReference in packageReferences)
                {
                    // If the package is referenced more than once (bizarrely, this happens one of our test inputs), only keep the highest version
                    var highestVersion = packageReferences
                        .Where(r => r.Name.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.GetNuGetVersion())
                        .Max();
                    if (highestVersion > packageReference.GetNuGetVersion())
                    {
                        Logger.LogInformation("Marking package {NuGetPackage} for removal because it is referenced elsewhere in the project with a higher version", packageReference);
                        _packagesToRemove.Add(packageReference);
                        continue;
                    }

                    // If the package is referenced transitively, mark for removal
                    var lockFileTarget = GetLockFileTarget(restoreOutput.LockFilePath);
                    if (lockFileTarget.Libraries.Any(l => l.Dependencies.Any(d => ReferenceSatisfiesDependency(d, packageReference, true))))
                    {
                        Logger.LogInformation("Marking package {PackageName} for removal because it appears to be a transitive dependency", packageReference.Name);
                        _packagesToRemove.Add(packageReference);
                        continue;
                    }

                    // If the package is in a package map, mark for removal and add appropriate packages for addition
                    var maps = packageMaps.Where(m => m.ContainsReference(packageReference.Name, packageReference.Version));
                    foreach (var map in maps)
                    {
                        if (map != null)
                        {
                            possibleBreakingChanges = true;
                            Logger.LogInformation("Marking package {PackageName} for removal based on package mapping configuration {PackageMapSet}", packageReference.Name, map.PackageSetName);
                            _packagesToRemove.Add(packageReference);
                            _packagesToAdd.AddRange(map.NetCorePackages);
                            continue;
                        }
                    }

                    // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                    if (await DoesPackageSupportTargetFrameworkAsync(packageReference, restoreOutput.PackageCachePath, token).ConfigureAwait(false))
                    {
                        Logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, _targetFramework);
                        continue;
                    }
                    else
                    {
                        // If the package won't work on the target Framework, check newer versions of the package
                        var updatedReference = await GetUpdatedPackageVersionAsync(packageReference, restoreOutput.PackageCachePath, token).ConfigureAwait(false);
                        if (updatedReference == null)
                        {
                            Logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, _targetFramework);
                        }
                        else
                        {
                            Logger.LogInformation("Marking package {NuGetPackage} for removal because it doesn't support the target framework but a newer version ({Version}) does", packageReference, updatedReference.Version);
                            var newMajorVersion = updatedReference.GetNuGetVersion()?.Major;
                            var oldMajorVersion = packageReference.GetNuGetVersion()?.Major;

                            if (newMajorVersion != oldMajorVersion)
                            {
                                Logger.LogWarning("Package {NuGetPackage} has been upgraded across major versions ({OldVersion} -> {NewVersion}) which may introduce breaking changes", packageReference.Name, oldMajorVersion, newMajorVersion);
                                possibleBreakingChanges = true;
                            }

                            _packagesToRemove.Add(packageReference);
                            _packagesToAdd.Add(updatedReference);
                            continue;
                        }
                    }
                }

                // If the project doesn't include a reference to the analyzer package, mark it for addition
                if (!packageReferences.Any(r => AnalyzerPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    // Use the analyzer package version from configuration if specified, otherwise get the latest version.
                    // When looking for the latest analyzer version, use the analyzer package source from configuration
                    // if one is specified, otherwise just use the package sources from the project being analyzed.
                    var analyzerPackageVersion = _analyzerPackageVersion is not null
                        ? NuGetVersion.Parse(_analyzerPackageVersion)
                        : await _packageLoader.GetLatestVersionAsync(AnalyzerPackageName, true, _analyzerPackageSource is null ? null : new[] { _analyzerPackageSource }, token).ConfigureAwait(false);
                    if (analyzerPackageVersion is not null)
                    {
                        Logger.LogInformation("Reference to analyzer package ({AnalyzerPackageName}, version {AnalyzerPackageVersion}) needs added", AnalyzerPackageName, analyzerPackageVersion);
                        _packagesToAdd.Add(new NuGetReference(AnalyzerPackageName, analyzerPackageVersion.ToString()));
                    }
                    else
                    {
                        Logger.LogWarning("Analyzer NuGet package reference cannot be added because the package cannot be found");
                    }
                }
                else
                {
                    Logger.LogDebug("Reference to analyzer package ({AnalyzerPackageName}) already exists", AnalyzerPackageName);
                }
            }
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", Options.ProjectPath);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}", BuildBreakRisk.Unknown);
            }

            _packagesToAdd = _packagesToAdd.Distinct().ToList();

            if (_packagesToRemove.Count == 0 && _packagesToAdd.Count == 0)
            {
                Logger.LogInformation("No package updates needed");
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }
            else
            {
                if (_packagesToRemove.Count > 0)
                {
                    Logger.LogInformation($"Packages to be removed:\n{string.Join('\n', _packagesToRemove)}");
                }

                if (_packagesToAdd.Count > 0)
                {
                    Logger.LogInformation($"Packages to be addded:\n{string.Join('\n', _packagesToAdd)}");
                }

                return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"{_packagesToRemove.Count} packages need removed and {_packagesToAdd.Count} packages need added", possibleBreakingChanges ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
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

                projectFile.RemovePackages(_packagesToRemove);
                projectFile.AddPackages(_packagesToAdd.Distinct());

                await projectFile.SaveAsync(token).ConfigureAwait(false);

                await RemoveTransitiveDependenciesAsync(context, token).ConfigureAwait(false);

                return new MigrationStepApplyResult(MigrationStepStatus.Complete, "Packages updated");
            }
            catch (Exception)
            {
                Logger.LogCritical("Invalid project: {ProjectPath}", Options.ProjectPath);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}");
            }
        }

        private async Task RemoveTransitiveDependenciesAsync(IMigrationContext context, CancellationToken token)
        {
            // After updating package versions and applying package maps, there may be more transitive dependencies that need removed.
            // Do a second scan for transitive dependencies and remove any that are found.
            Logger.LogDebug("Restoring updated packages");

            var project = context.Project.Required();

            var restoreOutput = await _packageRestorer.RestorePackagesAsync(_logRestoreOutput, context, token).ConfigureAwait(false);
            if (restoreOutput.LockFilePath is null)
            {
                Logger.LogWarning("Unable to restore packages for project {ProjectPath}", project.FilePath);
            }
            else
            {
                var lockFileTarget = GetLockFileTarget(restoreOutput.LockFilePath);

                var packageReferences = project.PackageReferences;
                var transitiveDependencies = new List<NuGetReference>();

                foreach (var reference in packageReferences)
                {
                    if (lockFileTarget.Libraries.Any(l => l.Dependencies.Any(d => ReferenceSatisfiesDependency(d, reference, true))))
                    {
                        Logger.LogInformation("Removing {PackageName} because, after package updates, it is included transitively", reference.Name);
                        transitiveDependencies.Add(reference);
                    }
                }

                if (transitiveDependencies.Any())
                {
                    var file = project.GetFile();

                    file.RemovePackages(transitiveDependencies);

                    await file.SaveAsync(token).ConfigureAwait(false);
                }
            }
        }

        private async Task<NuGetReference?> GetUpdatedPackageVersionAsync(NuGetReference packageReference, string? packageCachePath, CancellationToken token)
        {
            var latestMinorVersions = await _packageLoader.GetNewerVersionsAsync(packageReference.Name, new NuGetVersion(packageReference.GetNuGetVersion()), true, token).ConfigureAwait(false);
            NuGetReference? updatedReference = null;
            foreach (var newerPackage in latestMinorVersions.Select(v => new NuGetReference(packageReference.Name, v.ToString())))
            {
                if (await DoesPackageSupportTargetFrameworkAsync(newerPackage, packageCachePath, token).ConfigureAwait(false))
                {
                    Logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", newerPackage, _targetFramework);
                    updatedReference = newerPackage;
                    break;
                }
            }

            return updatedReference;
        }

        private async Task<IEnumerable<NuGetFramework>> GetTargetFrameworksAsync(PackageArchiveReader packageArchive, CancellationToken token)
        {
            var frameworksNames = new List<NuGetFramework>();

            // Add any target framework there are libraries for
            var libraries = await packageArchive.GetLibItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(libraries.Select(l => l.TargetFramework));

            // Add any target framework there are dependencies for
            var dependencies = await packageArchive.GetPackageDependenciesAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(dependencies.Select(d => d.TargetFramework));

            // Add any target framework there are reference assemblies for
            var refs = await packageArchive.GetReferenceItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(refs.Select(r => r.TargetFramework));

            var ret = frameworksNames.Distinct();
            Logger.LogDebug("Found target frameworks for package {NuGetPackage}: {TargetFrameworks}", (await packageArchive.GetIdentityAsync(token).ConfigureAwait(false)).ToString(), ret);
            return ret;
        }

        private async Task<List<NuGetPackageMap>> LoadPackageMapsAsync(CancellationToken token)
        {
            var maps = new List<NuGetPackageMap>();

            if (Directory.Exists(_packageMapSearchPath))
            {
                var mapPaths = Directory.GetFiles(_packageMapSearchPath, PackageMapExtension, SearchOption.AllDirectories);

                foreach (var mapPath in mapPaths)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        using var config = File.OpenRead(mapPath);
                        var newMaps = await JsonSerializer.DeserializeAsync<IEnumerable<NuGetPackageMap>>(config, cancellationToken: token).ConfigureAwait(false);
                        if (newMaps != null)
                        {
                            maps.AddRange(newMaps);
                            Logger.LogDebug("Loaded {MapCount} package maps from {PackageMapPath}", newMaps.Count(), mapPath);
                        }
                    }
                    catch (JsonException exc)
                    {
                        Logger.LogDebug(exc, "File {PackageMapPath} is not a valid package map file", mapPath);
                    }
                }

                Logger.LogDebug("Loaded {MapCount} package maps", maps.Count);
            }
            else
            {
                Logger.LogError("Package map search path ({PackageMapSearchPath}) not found", _packageMapSearchPath);
                throw new InvalidOperationException($"Package map search path ({_packageMapSearchPath}) not found");
            }

            return maps;
        }

        private async Task<bool> DoesPackageSupportTargetFrameworkAsync(NuGetReference packageReference, string? cachePath, CancellationToken token)
        {
            using var packageArchive = await _packageLoader.GetPackageArchiveAsync(packageReference, token, cachePath).ConfigureAwait(false);

            if (packageArchive is null)
            {
                return false;
            }

            var packageFrameworks = await GetTargetFrameworksAsync(packageArchive, token).ConfigureAwait(false);
            return packageFrameworks.Any(f => DefaultCompatibilityProvider.Instance.IsCompatible(_targetFramework, f));
        }

        private static bool ReferenceSatisfiesDependency(PackageDependency dependency, NuGetReference packageReference, bool minVersionMatchOnly)
        {
            // If the dependency's name doesn't match the reference's name, return false
            if (!dependency.Id.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var packageVersion = packageReference.GetNuGetVersion();
            if (packageVersion == null)
            {
                throw new InvalidOperationException("Package references from a lock file should always have a specific version");
            }

            // Return false if the reference's version falls outside of the dependency range
            var versionRange = dependency.VersionRange;
            if (versionRange.HasLowerBound && packageVersion < versionRange.MinVersion)
            {
                return false;
            }

            if (versionRange.HasUpperBound && packageVersion > versionRange.MaxVersion)
            {
                return false;
            }

            // In some cases (looking for transitive dependencies), it's interesting to only match packages that are the minimum version
            if (minVersionMatchOnly && versionRange.HasLowerBound && packageVersion != versionRange.MinVersion)
            {
                return false;
            }

            // Otherwise, return true
            return true;
        }

        private LockFileTarget GetLockFileTarget(string lockFilePath) =>
            LockFileUtilities.GetLockFile(lockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets.First(t => t.TargetFramework.DotNetFrameworkName.Equals(_targetFramework.DotNetFrameworkName, StringComparison.Ordinal));
    }
}
