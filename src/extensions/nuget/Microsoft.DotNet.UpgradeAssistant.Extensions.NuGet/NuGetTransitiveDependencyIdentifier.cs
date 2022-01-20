﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
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
        private readonly IEnumerable<PackageSource> _packageSources;
        private readonly ISettings _settings;
        private readonly SourceCacheContext _context;
        private readonly ILogger<NuGetTransitiveDependencyIdentifier> _logger;

        public NuGetTransitiveDependencyIdentifier(
            IEnumerable<PackageSource> packageSources,
            ISettings settings,
            SourceCacheContext context,
            ILogger<NuGetTransitiveDependencyIdentifier> logger)
        {
            _packageSources = packageSources ?? throw new ArgumentNullException(nameof(packageSources));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var path = Path.Combine(Path.GetTempPath(), "dotnet-ua", "restores", Guid.NewGuid().ToString(), "project.txt");

            var spec = new PackageSpec(tfmInfo)
            {
                Dependencies = packages.Select(i => new LibraryDependency
                {
                    LibraryRange = new LibraryRange(i.Name, new VersionRange(i.GetNuGetVersion()), LibraryDependencyTarget.Package),
                }).ToList(),
                RestoreMetadata = new()
                {
                    ProjectPath = path,
                    ProjectName = Path.GetFileNameWithoutExtension(path),
                    ProjectStyle = ProjectStyle.PackageReference,
                    ProjectUniqueName = path,
                    OutputPath = Path.GetTempPath(),
                    OriginalTargetFrameworks = tfms.Select(tfm => tfm.ToFullString()).ToArray(),
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

            var requestProvider = new DependencyGraphSpecRequestProvider(new RestoreCommandProvidersCache(), dependencyGraphSpec);

            var restoreArgs = new RestoreArgs
            {
                AllowNoOp = true,
                CacheContext = _context,
                CachingSourceProvider = new CachingSourceProvider(new PackageSourceProvider(_settings)),
                Log = new NuGetLogger(_logger),
            };

            // Create requests from the arguments
            var requests = await requestProvider.CreateRequests(restoreArgs).ConfigureAwait(false);

            // Restore the package without generating extra files
            var result = await RestoreRunner.RunWithoutCommit(requests, restoreArgs).ConfigureAwait(false);

            if (result.Count == 0)
            {
                return null;
            }

            return result[0].Result.RestoreGraphs.FirstOrDefault();
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

            public IEnumerator<NuGetReference> GetEnumerator()
            {
                foreach (var item in _graph.Flattened)
                {
                    var library = item.Data.Match.Library;

                    yield return new(library.Name, library.Version.ToString());
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<IGrouping<NuGetReference, NuGetReference>> IEnumerable<IGrouping<NuGetReference, NuGetReference>>.GetEnumerator()
            {
                foreach (var item in _graph.Flattened)
                {
                    yield return new ResolveResultGrouping(item.Data);
                }
            }

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

                public IEnumerator<NuGetReference> GetEnumerator()
                {
                    foreach (var dependency in _result.Dependencies)
                    {
                        yield return new(dependency.Name, dependency.LibraryRange.VersionRange.ToString());
                    }
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }
    }
}
