// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

using NuGet.Commands;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetTransitiveDependencyIdentifier : ITransitiveDependencyIdentifier
    {
        private static readonly ConcurrentDictionary<string, LibraryDependency> _packageDependencies = new();

        private readonly IEnumerable<PackageSource> _packageSources;
        private readonly ISettings _settings;
        private readonly SourceCacheContext _context;
        private readonly NuGetLogger _logger;
        private readonly CachingSourceProvider _cachingSourceProvider;
        private readonly RestoreCommandProvidersCache _providerCache = new();

        public NuGetTransitiveDependencyIdentifier(
            IEnumerable<PackageSource> packageSources,
            ISettings settings,
            SourceCacheContext context,
            ILogger<NuGetTransitiveDependencyIdentifier> logger)
        {
            _packageSources = packageSources ?? throw new ArgumentNullException(nameof(packageSources));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = new NuGetLogger(logger);
            _cachingSourceProvider = new CachingSourceProvider(new PackageSourceProvider(_settings));
        }

        public async Task<TransitiveClosureCollection> GetTransitiveDependenciesAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
        {
            if (packages is null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            if (tfms is null)
            {
                throw new ArgumentNullException(nameof(tfms));
            }

            var graph = await RestoreProjectAsync(packages, tfms, token).ConfigureAwait(false);

            if (graph is null)
            {
                return TransitiveClosureCollection.Empty;
            }

            return new(new TargetGraphLookup(graph));
        }

        private async Task<RestoreTargetGraph?> RestoreProjectAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
        {
            var tfmInfo = tfms.Select(tfm => new TargetFrameworkInformation { FrameworkName = NuGetFramework.Parse(tfm.ToFullString()) }).ToList();

            // Create a project in a unique and temporary directory
            var path = CreateUniquePath();
            var spec = new PackageSpec(tfmInfo)
            {
                Dependencies = packages.Select(i =>
                {
                    var lookupKey = $@"{i.Name}|{i.GetNuGetVersion()?.ToString() ?? "null"}";
                    return _packageDependencies.GetOrAdd(lookupKey, new LibraryDependency
                    {
                        LibraryRange = new LibraryRange(i.Name, new VersionRange(i.GetNuGetVersion()), LibraryDependencyTarget.Package),
                    });
                }).ToList(),
                RestoreMetadata = new()
                {
                    ProjectPath = path,
                    ProjectName = Path.GetFileNameWithoutExtension(path),
                    ProjectStyle = ProjectStyle.PackageReference,
                    ProjectUniqueName = path,
                    OutputPath = Path.GetTempPath(),
                    OriginalTargetFrameworks = tfms.Select(tfm => tfm.ToFullString()).ToList(),
                    ConfigFilePaths = _settings.GetConfigFilePaths(),
                    PackagesPath = SettingsUtility.GetGlobalPackagesFolder(_settings),
                    Sources = _packageSources.ToList(),
                    FallbackFolders = SettingsUtility.GetFallbackPackageFolders(_settings).ToList(),
                },
                FilePath = path,
                Name = Path.GetFileNameWithoutExtension(path),
            };

            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(spec);
            dependencyGraphSpec.AddRestore(spec.RestoreMetadata.ProjectUniqueName);

            var requestProvider = new DependencyGraphSpecRequestProvider(_providerCache, dependencyGraphSpec);

            var restoreArgs = new RestoreArgs
            {
                AllowNoOp = true,
                CacheContext = _context,
                CachingSourceProvider = _cachingSourceProvider,
                Log = _logger,
            };

            // Create requests from the arguments
            var requests = await requestProvider.CreateRequests(restoreArgs).ConfigureAwait(false);

            // Restore the package without generating extra files
            var result = await RestoreRunner.RunWithoutCommit(requests, restoreArgs).ConfigureAwait(false);
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections
            return result?.FirstOrDefault()?.Result.RestoreGraphs.FirstOrDefault();
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections

            static string CreateUniquePath()
            {
                const string UniquePathPart1 = "dotnet-ua";
                const string UniquePathPart2 = "restores";
                const string UniquePathFilename = "project.txt";

                return Path.Combine(Path.GetTempPath(), UniquePathPart1, UniquePathPart2, Guid.NewGuid().ToString(), UniquePathFilename);
            }
        }

        private class TargetGraphLookup : ILookup<NuGetReference, NuGetReference>
        {
            private readonly RestoreTargetGraph _graph;

            public TargetGraphLookup(RestoreTargetGraph graph)
            {
                _graph = graph;
            }

            public IEnumerable<NuGetReference> this[NuGetReference key] => TryFind(key, out var result) ? result : Enumerable.Empty<NuGetReference>();

            public int Count => _graph.Flattened.Count;

            public bool Contains(NuGetReference key) => TryFind(key, out _);

            public IEnumerator<NuGetReference> GetEnumerator() => _graph.Flattened.Select(item => new NuGetReference(item.Data.Match.Library.Name, item.Data.Match.Library.Version.ToString())).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<IGrouping<NuGetReference, NuGetReference>> IEnumerable<IGrouping<NuGetReference, NuGetReference>>.GetEnumerator() => _graph.Flattened.Select(item => new ResolveResultGrouping(item.Data)).GetEnumerator();

            private bool TryFind(NuGetReference package, [MaybeNullWhen(false)] out ResolveResultGrouping result)
            {
                foreach (var item in _graph.Flattened)
                {
                    if (string.Equals(item.Data.Match.Library.Name, package.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        result = new(item.Data);
                        return true;
                    }
                }

                result = default;
                return false;
            }

            private class ResolveResultGrouping : IGrouping<NuGetReference, NuGetReference>
            {
                private readonly RemoteResolveResult _result;

                public ResolveResultGrouping(RemoteResolveResult result)
                {
                    _result = result;
                    Key = new(_result.Match.Library.Name, _result.Match.Library.Version.ToString());
                }

                public NuGetReference Key { get; }

                public IEnumerator<NuGetReference> GetEnumerator() => _result.Dependencies.Select(dependency => new NuGetReference(dependency.Name, dependency.LibraryRange.VersionRange.ToString())).GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }
    }
}
