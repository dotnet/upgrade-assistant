// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public sealed class PackageLoader : IPackageLoader, IPackageDownloader, IPackageCreator
    {
        private const int MaxRetries = 3;

        private readonly Dictionary<PackageSource, SourceRepository> _sourceRepositoryCache;
        private readonly NuGetLogger _nugetLogger;
        private readonly SourceCacheContext _context;
        private readonly IEnumerable<PackageSource> _packageSource;
        private readonly IOptions<NuGetDownloaderOptions> _options;
        private readonly ILogger<PackageLoader> _logger;

        public PackageLoader(
            SourceCacheContext context,
            IEnumerable<PackageSource> packageSource,
            IOptions<NuGetDownloaderOptions> options,
            ILogger<PackageLoader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
            _packageSource = packageSource;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sourceRepositoryCache = new Dictionary<PackageSource, SourceRepository>();

            _nugetLogger = new NuGetLogger(logger);
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

        public IAsyncEnumerable<NuGetReference> GetNewerVersionsAsync(NuGetReference reference, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, CancellationToken token)
        {
            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return SearchByNameAsync(reference.Name, tfms, options, currentVersion: reference.GetNuGetVersion(), _packageSource, token: token);
        }

        public Task<NuGetReference?> GetLatestVersionAsync(string packageName, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, CancellationToken token)
            => GetLatestVersionAsync(packageName, tfms, options, default, token);

        private async Task<NuGetReference?> GetLatestVersionAsync(string packageName, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, IEnumerable<PackageSource>? sources, CancellationToken token)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var result = SearchByNameAsync(packageName, tfms, options, null, sources, token: token);

            return await result.LastOrDefaultAsync(token).ConfigureAwait(false);
        }

        private async IAsyncEnumerable<NuGetReference> SearchByNameAsync(string name, IEnumerable<TargetFrameworkMoniker> tfms, PackageSearchOptions options, NuGetVersion? currentVersion = null, IEnumerable<PackageSource>? sources = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            var results = new List<IPackageSearchMetadata>();

            if (sources is null)
            {
                sources = _packageSource;
            }

            foreach (var source in sources)
            {
                try
                {
                    var metadata = await GetSourceRepository(source).GetResourceAsync<PackageMetadataResource>(token).ConfigureAwait(false);
                    var searchResults = await CallWithRetryAsync(() => metadata.GetMetadataAsync(name, includePrerelease: options.Prerelease, includeUnlisted: options.Unlisted, _context, _nugetLogger, token)).ConfigureAwait(false);

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

            // Switch to an IEnumerable here because consumers of this API
            await foreach (var result in FilterSearchResultsAsync(name, results, tfms, currentVersion, options.LatestMinorAndBuildOnly, token).WithCancellation(token))
            {
                yield return result;
            }
        }

        internal IAsyncEnumerable<NuGetReference> FilterSearchResultsAsync(
            string name,
            IReadOnlyCollection<IPackageSearchMetadata> searchResults,
            IEnumerable<TargetFrameworkMoniker> tfms,
            NuGetVersion? currentVersion = null,
            bool latestMinorAndBuildOnly = false,
            CancellationToken token = default)
        {
            if (searchResults is null || searchResults.Count == 0)
            {
                return AsyncEnumerable.Empty<NuGetReference>();
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

            return results.ToAsyncEnumerable()
                .WhereAwait(async r =>
                {
                    // If the package has no dependency sets, then include it (as it may be build tools, analyzers, etc.).
                    if (!r.DependencySets.Any())
                    {
                        return true;
                    }

                    // If the package has dependency sets, only include it if there are dependency sets for each
                    // of the TFMs that need to be supported.
                    var unsupported = tfmSet;
                    foreach (var dep in r.DependencySets)
                    {
                        if (dep.TargetFramework.IsAny)
                        {
                            // If dependencies have an 'Any' target, download the package and examine its actual contents to determine
                            // framework support.
                            return await DoesPackageSupportTargetFrameworksAsync(new NuGetReference(r.Identity.Id, r.Identity.Version.ToString()), tfms, CancellationToken.None).ConfigureAwait(false);
                        }
                        else
                        {
                            // If the dependencies have a particular target, note that that target is supported
                            foreach (var t in unsupported)
                            {
                                if (DefaultCompatibilityProvider.Instance.IsCompatible(t, dep.TargetFramework))
                                {
                                    unsupported = unsupported.Remove(t);
                                }
                            }
                        }
                    }

                    // Return true if all the needed TFMs were supported
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

        private Stream? GetCachedPackage(NuGetReference packageReference)
        {
            // First look in the local NuGet cache for the archive
            if (_options.Value.CachePath is string cachePath)
            {
                var archivePath = Path.Combine(cachePath, packageReference.Name, packageReference.Version, $"{packageReference.Name}.{packageReference.Version}.nupkg");
                if (File.Exists(archivePath))
                {
                    _logger.LogDebug("NuGet package {NuGetPackage} loaded from {PackagePath}", packageReference, archivePath);
                    return File.Open(archivePath, FileMode.Open);
                }
                else
                {
                    _logger.LogDebug("NuGet package {NuGetPackage} not found in package cache", packageReference);
                }
            }

            return null;
        }

        private async Task<Stream> GetPackageStream(NuGetReference packageReference, string source, CancellationToken token)
        {
            var ms = new MemoryStream();
            var packageSource = new[] { new PackageSource(source) };

            await DownloadPackageToStreamAsync(packageReference, ms, packageSource, token).ConfigureAwait(false);

            ms.Position = 0;

            return ms;
        }

        private async Task<bool> DownloadPackageToStreamAsync(NuGetReference packageReference, Stream stream, IEnumerable<PackageSource>? sources, CancellationToken token)
        {
            // Attempt to download the package from the sources
            var packageVersion = packageReference.GetNuGetVersion();
            var packageSources = sources ?? _packageSource;

            foreach (var source in packageSources)
            {
                var repo = GetSourceRepository(source);
                try
                {
                    var packageFinder = await repo.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);
                    if (await packageFinder.DoesPackageExistAsync(packageReference.Name, packageVersion, _context, (NuGetLogger?)_nugetLogger, token).ConfigureAwait(false))
                    {
                        if (await packageFinder.CopyNupkgToStreamAsync(packageReference.Name, packageVersion, stream, _context, (NuGetLogger?)_nugetLogger, token).ConfigureAwait(false))
                        {
                            _logger.LogDebug("Package {NuGetPackage} downloaded from feed {NuGetFeed}", packageReference, source.Source);
                            return true;
                        }
                        else
                        {
                            _logger.LogDebug("Failed to download package {NuGetPackage} from source {NuGetFeed}", packageReference, source.Source);
                        }
                    }
                }
                catch (NuGetProtocolException)
                {
                    _logger.LogWarning("Failed to get package finder from source {PackageSource} due to a NuGet protocol error, skipping this source", source.Source);
                    _logger.LogInformation("If NuGet packages are coming from an authenticated source, Upgrade Assistant requires a .NET Core-compatible v2 credential provider be installed. To authenticate with an Azure DevOps NuGet source, for example, see https://github.com/microsoft/artifacts-credprovider#setup");
                }
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Package archive will clean up streams")]
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

            if (GetCachedPackage(packageReference) is Stream cached)
            {
                return new PackageArchiveReader(cached, false);
            }

            var ms = new MemoryStream();

            if (await DownloadPackageToStreamAsync(packageReference, ms, sources: default, token).ConfigureAwait(false))
            {
                return new PackageArchiveReader(ms, false);
            }

            await ms.DisposeAsync().ConfigureAwait(false);
            return null;
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

        public async Task<NuGetPackageMetadata?> GetPackageMetadata(NuGetReference reference, CancellationToken token)
        {
            var archive = await GetPackageArchiveAsync(reference, token).ConfigureAwait(false);

            if (archive is null)
            {
                return null;
            }

            var nuspec = await archive.GetNuspecReaderAsync(token).ConfigureAwait(false);

            return new NuGetPackageMetadata
            {
                Owners = nuspec.GetOwners()
            };
        }

        public async Task<bool> DownloadPackageToDirectoryAsync(string path, NuGetReference nugetReference, string source, CancellationToken token)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
            }

            if (nugetReference is null)
            {
                throw new ArgumentNullException(nameof(nugetReference));
            }

            using var stream = GetCachedPackage(nugetReference) ?? await GetPackageStream(nugetReference, source, token).ConfigureAwait(false);

            if (stream is null)
            {
                _logger.LogWarning("Could not find {Package} [{Version}]", nugetReference.Name, nugetReference.Version);
                return false;
            }

            try
            {
                using var zip = new ZipArchive(stream);

                zip.ExtractToDirectory(path);

                return true;
            }
            catch (InvalidDataException)
            {
            }
            catch (IOException)
            {
            }

            _logger.LogError("Invalid package file.");

            return false;
        }

        public bool CreateArchive(IExtensionInstance extension, string? packageType, Stream stream)
        {
            if (extension is null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (!PackageIdValidator.IsValidPackageId(extension.Name))
            {
                _logger.LogError("Invalid extension name to create package.");
                return false;
            }

            if (extension.Version is null)
            {
                _logger.LogError("Version must not be empty to create package.");
                return false;
            }

            if (string.IsNullOrEmpty(extension.Description))
            {
                _logger.LogError("Description must not be empty to create package");
                return false;
            }

            if (extension.Authors.Count == 0)
            {
                _logger.LogError("Must include an author to create a package");
                return false;
            }

            var builder = new PackageBuilder
            {
                Id = extension.Name,
                Version = NuGetVersion.Parse(extension.Version.ToString()),
                Description = extension.Description
            };

            if (packageType is not null)
            {
                builder.PackageTypes = new[] { new PackageType(packageType, new Version(1, 0, 0)) };
            }

            builder.Authors.AddRange(extension.Authors);
            builder.Files.Add(new EmptyFile(Path.Combine("content", "_.txt")));

            builder.Save(stream);

            return true;
        }

        private class EmptyFile : IPackageFile
        {
            public EmptyFile(string path)
            {
                TargetFramework = FrameworkNameUtility.ParseFrameworkNameFromFilePath(path, out var effective);
                Path = path;
                EffectivePath = effective;
            }

            public string Path { get; }

            public string EffectivePath { get; }

            public FrameworkName TargetFramework { get; }

            public NuGetFramework NuGetFramework => null!;

            public DateTimeOffset LastWriteTime => DateTimeOffset.UtcNow;

            public Stream GetStream() => Stream.Null;
        }

        public async ValueTask<NuGetReference?> GetNuGetReference(string name, string? version, string source, CancellationToken token)
        {
            if (version is null)
            {
                return await GetLatestVersionAsync(name, Enumerable.Empty<TargetFrameworkMoniker>(), new(), new[] { new PackageSource(source) }, token).ConfigureAwait(false);
            }
            else
            {
                return new NuGetReference(name, version);
            }
        }
    }
}
