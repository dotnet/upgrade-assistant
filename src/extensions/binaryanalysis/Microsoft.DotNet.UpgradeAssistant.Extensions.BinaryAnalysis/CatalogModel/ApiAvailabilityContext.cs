// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class ApiAvailabilityContext
{
    public static ApiAvailabilityContext Create(ApiCatalogModel catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new ApiAvailabilityContext(catalog);
    }

    private readonly Dictionary<NuGetFramework, int> _frameworkIds;
    private readonly Dictionary<int, HashSet<int>> _frameworkAssemblies;
    private readonly Dictionary<int, IReadOnlyList<PackageFolder>> _packageFolders;

    private ApiAvailabilityContext(ApiCatalogModel catalog)
    {
        Catalog = catalog;
        _frameworkIds = new Dictionary<NuGetFramework, int>();
        _frameworkAssemblies = catalog.Frameworks.Select(fx => (fx.Id, Assemblies: fx.Assemblies.Select(a => a.Id).ToHashSet()))
                                                 .ToDictionary(t => t.Id, t => t.Assemblies);

        _packageFolders = new Dictionary<int, IReadOnlyList<PackageFolder>>();

        var nugetFrameworks = new Dictionary<int, NuGetFramework>();

        foreach (var fx in catalog.Frameworks)
        {
            var nugetFramework = NuGetFramework.Parse(fx.Name);
            if (nugetFramework.IsPCL || fx.Name is "monotouch" or "xamarinios10")
            {
                continue;
            }

            nugetFrameworks.Add(fx.Id, nugetFramework);
            _frameworkIds.Add(nugetFramework, fx.Id);
        }

        foreach (var package in catalog.Packages)
        {
            var folders = new Dictionary<NuGetFramework, PackageFolder>();

            foreach (var (framework, assembly) in package.Assemblies)
            {
                if (nugetFrameworks.TryGetValue(framework.Id, out var targetFramework))
                {
                    if (!folders.TryGetValue(targetFramework, out var folder))
                    {
                        folder = new PackageFolder(targetFramework, framework);
                        folders.Add(targetFramework, folder);
                    }

                    folder.Assemblies.Add(assembly);
                }
            }

            if (folders.Count > 0)
            {
                _packageFolders.Add(package.Id, folders.Values.ToArray());
            }
        }
    }

    public ApiCatalogModel Catalog { get; }

    public ApiAvailability GetAvailability(ApiModel api)
    {
        var result = new List<ApiFrameworkAvailability>();

        foreach (var nugetFramework in _frameworkIds.Keys)
        {
            var availability = GetAvailability(api, nugetFramework);
            if (availability is not null)
            {
                result.Add(availability);
            }
        }

        return new ApiAvailability(result.ToArray());
    }

    public ApiFrameworkAvailability? GetAvailability(ApiModel api, NuGetFramework nugetFramework)
    {
        // Try to resolve an in-box assembly
        if (_frameworkIds.TryGetValue(nugetFramework, out var frameworkId))
        {
            if (_frameworkAssemblies.TryGetValue(frameworkId, out var frameworkAssemblies))
            {
                foreach (var declaration in api.Declarations)
                {
                    if (frameworkAssemblies.Contains(declaration.Assembly.Id))
                    {
                        return new ApiFrameworkAvailability(nugetFramework, declaration, null, null);
                    }
                }
            }
        }

        // Try to resolve an assembly in a package for the given framework
        foreach (var declaration in api.Declarations)
        {
            foreach (var (package, _) in declaration.Assembly.Packages)
            {
                if (_packageFolders.TryGetValue(package.Id, out var folders))
                {
                    var folder = NuGetFrameworkUtility.GetNearest(folders, nugetFramework);
                    if (folder is not null && folder.Assemblies.Contains(declaration.Assembly))
                    {
                        return new ApiFrameworkAvailability(nugetFramework, declaration, package, folder.TargetFramework);
                    }
                }
            }
        }

        return null;
    }

    private sealed class PackageFolder : IFrameworkSpecific
    {
        public PackageFolder(NuGetFramework targetFramework, FrameworkModel framework)
        {
            TargetFramework = targetFramework;
            Framework = framework;
        }

        public NuGetFramework TargetFramework { get; }

        public FrameworkModel Framework { get; }

        public List<AssemblyModel> Assemblies { get; } = new();
    }
}
