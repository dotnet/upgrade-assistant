// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
            _context = context;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyCollection<NuGetReference>> GetTransitiveDependenciesAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
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
                return Array.Empty<NuGetReference>();
            }

            return new GraphItemCollection(graph.Flattened);
        }

        public async Task<IEnumerable<NuGetReference>> RemoveTransitiveDependenciesAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
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
                return packages;
            }

            IEnumerable<LibraryDependency> GetPackageDependencies(NuGetReference package) => graph.Flattened.First(p => p.Key.Name.Equals(package.Name, StringComparison.OrdinalIgnoreCase)).Data.Dependencies;

            bool IsPackageInDependencySet(NuGetReference packageReference, IEnumerable<LibraryDependency> dependencies) => dependencies.Any(d => d.Name.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase));

            return packages.Where(package =>
            {
                return packages.Select(GetPackageDependencies).Any(dependencies => IsPackageInDependencySet(package, dependencies));
            });
        }

        private async Task<RestoreTargetGraph?> RestoreProjectAsync(IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfm, CancellationToken token)
        {
            var tfmInfo = tfm.Select(tfm => new TargetFrameworkInformation { FrameworkName = NuGetFramework.Parse(tfm.ToFullString()) }).ToList();

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
                    OriginalTargetFrameworks = new[] { tfm.ToString() },
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

        private class GraphItemCollection : IReadOnlyCollection<NuGetReference>
        {
            private readonly ISet<GraphItem<RemoteResolveResult>> _flattened;

            public GraphItemCollection(ISet<GraphItem<RemoteResolveResult>> flattened)
            {
                _flattened = flattened;
            }

            public int Count => _flattened.Count;

            public IEnumerator<NuGetReference> GetEnumerator()
            {
                foreach (var item in _flattened)
                {
                    var library = item.Data.Match.Library;

                    yield return new(library.Name, library.Version.ToString());
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
