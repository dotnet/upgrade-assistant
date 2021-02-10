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

        public PackageCollection(IEnumerable<NuGetReference> packages)
        {
            _packages = packages.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetPackageByName(string packageName, [MaybeNullWhen(false)] out NuGetReference nugetReference)
            => _packages.TryGetValue(packageName, out nugetReference);

        public IEnumerator<NuGetReference> GetEnumerator() => _packages.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
