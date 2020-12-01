using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace AspNetMigrator.Engine
{
    public sealed class PackageLoader : IPackageLoader, IDisposable
    {
        private const string DefaultPackageSource = "https://api.nuget.org/v3/index.json";
        private const int MaxRetries = 3;

        private readonly SourceCacheContext _cache;
        private readonly List<PackageSource> _packageSources;
        private readonly ILogger _logger;

        public PackageLoader(MigrateOptions options, ILogger<PackageLoader> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ProjectPath is null)
            {
                throw new ArgumentException("Project path must be set in MigrateOptions", nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new SourceCacheContext();
            _packageSources = GetPackageSources(Path.GetDirectoryName(options.ProjectPath));
        }

        public async Task<PackageArchiveReader?> GetPackageArchiveAsync(NuGetReference packageReference, CancellationToken token, string? cachePath = null)
        {
            if (packageReference is null)
            {
                throw new ArgumentNullException(nameof(packageReference));
            }

            if (packageReference.GetNuGetVersion() == null)
            {
                throw new ArgumentException("Package references must have specific versions to get package archives", nameof(packageReference));
            }

            // First look in the local NuGet cache for the archive
            if (cachePath != null)
            {
                var archivePath = Path.Combine(cachePath, packageReference.Name.ToLowerInvariant(), packageReference.Version, $"{packageReference.Name.ToLowerInvariant()}.{packageReference.Version}.nupkg");
                if (File.Exists(archivePath))
                {
                    _logger.LogDebug("NuGet package {NuGetPackage} loaded from {PackagePath}", packageReference, archivePath);
                    return new PackageArchiveReader(File.Open(archivePath, FileMode.Open), false);
                }
                else
                {
                    _logger.LogDebug("NuGet package {NuGetPackage} not found in package cache", packageReference);
                }
            }

            // Attempt to download the package from the sources
            var packageVersion = packageReference.GetNuGetVersion();
            foreach (var source in _packageSources)
            {
                var repo = Repository.Factory.GetCoreV3(source);
                var packageFinder = await repo.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
                if (await packageFinder.DoesPackageExistAsync(packageReference.Name, packageVersion, _cache, NuGet.Common.NullLogger.Instance, token).ConfigureAwait(false))
                {
                    var memoryStream = new MemoryStream();
                    if (await packageFinder.CopyNupkgToStreamAsync(packageReference.Name, packageVersion, memoryStream, _cache, NuGet.Common.NullLogger.Instance, token).ConfigureAwait(false))
                    {
                        _logger.LogDebug("Package {NuGetPackage} download from feed {NuGetFeed}", packageReference, source.Source);
                        return new PackageArchiveReader(memoryStream, false);
                    }
                    else
                    {
                        _logger.LogDebug("Failed to download package {NuGetPackage} from feed {NuGetFeed}", packageReference, source.Source);
                        memoryStream.Close();
                    }
                }
            }

            _logger.LogWarning("NuGet package {NuGetPackage} not found", packageReference);
            return null;
        }

        public async Task<IEnumerable<NuGetVersion>> GetNewerVersionsAsync(string packageName, NuGetVersion currentVersion, bool latestMinorAndBuildOnly, CancellationToken token)
        {
            var versions = new List<NuGetVersion>();

            // Query each package source for listed versions of the given package name
            foreach (var source in _packageSources)
            {
                var metadata = await Repository.Factory.GetCoreV3(source).GetResourceAsync<PackageMetadataResource>(token).ConfigureAwait(false);
                try
                {
                    var searchResults = await CallWithRetryAsync(() => metadata.GetMetadataAsync(packageName, includePrerelease: true, includeUnlisted: false, _cache, NuGet.Common.NullLogger.Instance, token)).ConfigureAwait(false);
                    versions.AddRange(searchResults.Select(r => r.Identity.Version));
                }
                catch (NuGetProtocolException)
                {
                    _logger.LogWarning("Failed to get package versions from source {PackageSource}", source.Source);
                }
            }

            // Filter to only include versions higher than the user's current version and,
            // optionally, only the highest minor/build for each major version
            var filteredVersions = versions.Distinct().Where(v => v > currentVersion);
            var versionsToReturn = latestMinorAndBuildOnly
                ? filteredVersions.GroupBy(v => v.Major).Select(g => g.Max()!)
                : filteredVersions;

            _logger.LogDebug("Found versions for package {PackageName}: {PackageVersions}", packageName, versionsToReturn);
            return versionsToReturn.OrderBy(v => v);
        }

        private List<PackageSource> GetPackageSources(string? projectDir)
        {
            var packageSources = new List<PackageSource>();
            if (projectDir != null)
            {
                var nugetSettings = Settings.LoadDefaultSettings(projectDir);
                var sourceProvider = new PackageSourceProvider(nugetSettings);
                packageSources.AddRange(sourceProvider.LoadPackageSources());
            }

            if (packageSources.Count == 0)
            {
                packageSources.Add(new PackageSource(DefaultPackageSource));
            }

            _logger.LogDebug("Found package sources: {PackageSources}", packageSources);
            return packageSources;
        }

        private async Task<T> CallWithRetryAsync<T>(Func<Task<T>> func)
        {
            for (var i = 0; i < MaxRetries; i++)
            {
                try
                {
                    var ret = await func.Invoke().ConfigureAwait(false);
                    return ret;
                }
                catch (NuGetProtocolException)
                {
                    if (i < MaxRetries - 1)
                    {
                        var delay = (int)(1000 * Math.Pow(2, i));
                        _logger.LogInformation("NuGet operation failed; retrying in {RetryTime} seconds", delay / 1000);
                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to execute NuGet action after {MaxRetries} attempts", MaxRetries);
                        throw;
                    }
                }
            }

            // The compiler doesn't believe me that the above code either always returns or always throws
            throw new InvalidOperationException("This should never be reached; fix the bug in PackageLoader");
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}
