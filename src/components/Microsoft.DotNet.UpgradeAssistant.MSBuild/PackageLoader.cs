// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public sealed class PackageLoader : IPackageLoader, IDisposable
    {
        private const int MaxRetries = 3;

        private readonly SourceCacheContext _cache;
        private readonly Lazy<IEnumerable<PackageSource>> _packageSources;
        private readonly ILogger<PackageLoader> _logger;
        private readonly NuGet.Common.ILogger _nugetLogger;
        private readonly Dictionary<PackageSource, SourceRepository> _sourceRepositoryCache;
        private readonly NuGetDownloaderOptions _options;

        public PackageLoader(
            UpgradeOptions upgradeOptions,
            INuGetPackageSourceFactory sourceFactory,
            ILogger<PackageLoader> logger,
            IOptions<NuGetDownloaderOptions> options)
        {
            if (upgradeOptions is null)
            {
                throw new ArgumentNullException(nameof(upgradeOptions));
            }

            if (sourceFactory is null)
            {
                throw new ArgumentNullException(nameof(sourceFactory));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (upgradeOptions.ProjectPath is null)
            {
                throw new ArgumentException("Project path must be set in UpgradeOptions", nameof(upgradeOptions));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nugetLogger = new NuGetLogger(logger);
            _cache = new SourceCacheContext();
            _packageSources = new Lazy<IEnumerable<PackageSource>>(() => sourceFactory.GetPackageSources(Path.GetDirectoryName(upgradeOptions.ProjectPath)));
            _sourceRepositoryCache = new Dictionary<PackageSource, SourceRepository>();
            _options = options.Value;
        }

        public async Task<bool> DoesPackageSupportTargetFrameworksAsync(NuGetReference packageReference, IEnumerable<TargetFrameworkMoniker> targetFrameworks, CancellationToken token)
        {
            using var packageArchive = await GetPackageArchiveAsync(packageReference, token).ConfigureAwait(false);

            if (packageArchive is null)
            {
                return false;
            }

            var packageFrameworks = await GetTargetFrameworksAsync(packageArchive, token).ConfigureAwait(false);

            return targetFrameworks.All(tfm => packageFrameworks.Any(f => DefaultCompatibilityProvider.Instance.IsCompatible(NuGetFramework.Parse(tfm.Name), f)));
        }

        public Task<IEnumerable<NuGetReference>> GetNewerVersionsAsync(NuGetReference reference, IEnumerable<TargetFrameworkMoniker> tfms, bool latestMinorAndBuildOnly, CancellationToken token)
        {
            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            return SearchByNameAsync(reference.Name, tfms, currentVersion: reference.GetNuGetVersion(), latestMinorAndBuildOnly: latestMinorAndBuildOnly, token: token);
        }

        public async Task<NuGetReference?> GetLatestVersionAsync(string packageName, IEnumerable<TargetFrameworkMoniker> tfms, bool includePreRelease, CancellationToken token)
        {
            var result = await SearchByNameAsync(packageName, tfms, includePreRelease, token: token).ConfigureAwait(false);

            return result.LastOrDefault();
        }

        private async Task<IEnumerable<NuGetReference>> SearchByNameAsync(string name, IEnumerable<TargetFrameworkMoniker> tfms, bool includePrerelease = false, NuGetVersion? currentVersion = null, bool latestMinorAndBuildOnly = false, CancellationToken token = default)
        {
            var results = new List<IPackageSearchMetadata>();

            foreach (var source in _packageSources.Value)
            {
                try
                {
                    var metadata = await GetSourceRepository(source).GetResourceAsync<PackageMetadataResource>(token).ConfigureAwait(false);
                    var searchResults = await CallWithRetryAsync(() => metadata.GetMetadataAsync(name, includePrerelease: includePrerelease, includeUnlisted: false, _cache, _nugetLogger, token)).ConfigureAwait(false);

                    results.AddRange(searchResults);
                }
                catch (NuGetProtocolException)
                {
                    _logger.LogWarning("Failed to get package versions from source {PackageSource} due to a NuGet protocol error", source.Source);
                    _logger.LogInformation("If NuGet packages are coming from an authenticated source, Upgrade Assistant requires a .NET Core-compatible v2 credential provider be installed. To authenticate with an Azure DevOps NuGet source, for example, see https://github.com/microsoft/artifacts-credprovider#setup");
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogWarning("Failed to get package versions from source {PackageSource} due to an HTTP error ({StatusCode})", source.Source, exc.StatusCode);
                }
            }

            return FilterSearchResults(name, results, tfms, currentVersion, latestMinorAndBuildOnly);
        }

        public static IEnumerable<NuGetReference> FilterSearchResults(
            string name,
            IReadOnlyCollection<IPackageSearchMetadata> searchResults,
            IEnumerable<TargetFrameworkMoniker> tfms,
            NuGetVersion? currentVersion = null,
            bool latestMinorAndBuildOnly = false)
        {
            if (searchResults is null || searchResults.Count == 0)
            {
                return Enumerable.Empty<NuGetReference>();
            }

            var tfmSet = ImmutableHashSet.CreateRange(tfms.Select(t => NuGetFramework.Parse(t.Name)));

            var results = searchResults
                .Where(r => currentVersion is null || r.Identity.Version > currentVersion);

            if (latestMinorAndBuildOnly)
            {
                results = results
                    .GroupBy(r => r.Identity.Version.Major)
                    .SelectMany(r =>
                    {
                        var max = r.Max(t => t.Identity.Version);

                        return r.Where(t => t.Identity.Version == max);
                    });
            }

            return results
                .Where(r =>
                {
                    var unsupported = tfmSet;

                    foreach (var dep in r.DependencySets)
                    {
                        foreach (var t in unsupported)
                        {
                            if (DefaultCompatibilityProvider.Instance.IsCompatible(t, dep.TargetFramework))
                            {
                                unsupported = unsupported.Remove(t);
                            }
                        }
                    }

                    return unsupported.IsEmpty;
                })
                .Select(r => r.Identity.Version)
                .OrderBy(v => v)
                .Select(v => new NuGetReference(name, v.ToNormalizedString()));
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

        private async Task<IEnumerable<NuGetFramework>> GetTargetFrameworksAsync(PackageArchiveReader packageArchive, CancellationToken token)
        {
            var frameworksNames = new List<NuGetFramework>();

            // Add any target framework there are libraries for
            var libraries = await packageArchive.GetLibItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(libraries.Select(l => l.TargetFramework));

            // Add any specific target framework there are dependencies for (do not add any, since
            // an 'any' dependency just means that does not necessarilly mean this package can work
            // with any target)
            var dependencies = await packageArchive.GetPackageDependenciesAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(dependencies.Select(d => d.TargetFramework).Where(f => !f.IsAny));

            // Add any target framework there are reference assemblies for
            var refs = await packageArchive.GetReferenceItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(refs.Select(r => r.TargetFramework));

            // If no frameworks are referenced, then assume the package works everywhere (it is likely
            // a package of analyzers or some other tooling)
            if (!frameworksNames.Any())
            {
                frameworksNames.Add(NuGetFramework.AnyFramework);
            }

            var ret = frameworksNames.Distinct();
            _logger.LogDebug("Found target frameworks for package {NuGetPackage}: {TargetFrameworks}", (await packageArchive.GetIdentityAsync(token).ConfigureAwait(false)).ToString(), ret);
            return ret;
        }

        private async Task<PackageArchiveReader?> GetPackageArchiveAsync(NuGetReference packageReference, CancellationToken token)
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
            if (_options.CachePath is string cachePath)
            {
                var archivePath = Path.Combine(cachePath, packageReference.Name, packageReference.Version, $"{packageReference.Name}.{packageReference.Version}.nupkg");
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
            foreach (var source in _packageSources.Value)
            {
                var repo = GetSourceRepository(source);
                try
                {
                    var packageFinder = await repo.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
                    if (await packageFinder.DoesPackageExistAsync(packageReference.Name, packageVersion, _cache, _nugetLogger, token).ConfigureAwait(false))
                    {
                        var memoryStream = new MemoryStream();
                        if (await packageFinder.CopyNupkgToStreamAsync(packageReference.Name, packageVersion, memoryStream, _cache, _nugetLogger, token).ConfigureAwait(false))
                        {
                            _logger.LogDebug("Package {NuGetPackage} downloaded from feed {NuGetFeed}", packageReference, source.Source);
                            return new PackageArchiveReader(memoryStream, false);
                        }
                        else
                        {
                            _logger.LogDebug("Failed to download package {NuGetPackage} from source {NuGetFeed}", packageReference, source.Source);
                            memoryStream.Close();
                        }
                    }
                }
                catch (NuGetProtocolException)
                {
                    _logger.LogWarning("Failed to get package finder from source {PackageSource} due to a NuGet protocol error, skipping this source", source.Source);
                    _logger.LogInformation("If NuGet packages are coming from an authenticated source, Upgrade Assistant requires a .NET Core-compatible v2 credential provider be installed. To authenticate with an Azure DevOps NuGet source, for example, see https://github.com/microsoft/artifacts-credprovider#setup");
                }
            }

            _logger.LogWarning("NuGet package {NuGetPackage} not found", packageReference);
            return null;
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }

        private SourceRepository GetSourceRepository(PackageSource source)
        {
            if (!_sourceRepositoryCache.TryGetValue(source, out var repository))
            {
                repository = Repository.Factory.GetCoreV3(source);
                _sourceRepositoryCache[source] = repository;
            }

            return repository;
        }
    }
}
