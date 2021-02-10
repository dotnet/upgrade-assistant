using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.UpgradeAssistant.Steps.Packages
{
    public class PackageCollection : IEnumerable<NuGetReference>
    {
        private readonly Dictionary<string, NuGetReference> _packages;

        public Dictionary<string, IEnumerable<NuGetReference>> DuplicatePackages { get; }

        public PackageCollection(IEnumerable<NuGetReference> packages)
        {
            // In the rare case a package is referenced more than once, only track the highest version
            var filteredPackages = packages.Where(p => !packages.Any(p1 => p1.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p1.GetNuGetVersion() > p.GetNuGetVersion()));
            _packages = filteredPackages.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // Track packages with multiple versions referenced so that analyzers can evaluate them
            DuplicatePackages = packages.Where(p => packages.Any(p1 => p1.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase) && p1.GetNuGetVersion() > p.GetNuGetVersion()))
                .Select(p => p.Name)
                .Distinct()
                .ToDictionary(n => n, n => packages.Where(p => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));
        }

        public bool TryGetPackageByName(string packageName, [MaybeNullWhen(false)] out NuGetReference nugetReference)
            => _packages.TryGetValue(packageName, out nugetReference);

        public IEnumerator<NuGetReference> GetEnumerator() => _packages.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
