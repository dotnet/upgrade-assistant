// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageCollection : IEnumerable<NuGetReference>
    {
        public ILookup<string, NuGetReference> Packages { get; }

        public PackageCollection(IEnumerable<NuGetReference> packages)
        {
            // In the rare case a package is referenced more than once, only track the highest version
            Packages = packages.ToLookup(p => p.Name);
        }

        public bool TryGetPackageByName(string packageName, [MaybeNullWhen(false)] out NuGetReference nugetReference)
        {
            var matches = Packages[packageName].OrderByDescending(p => p.GetNuGetVersion());

            if (!matches.Any())
            {
                nugetReference = null;
                return false;
            }

            nugetReference = matches.First();
            return true;
        }

        public IEnumerator<NuGetReference> GetEnumerator() => Packages.SelectMany(g => g).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
