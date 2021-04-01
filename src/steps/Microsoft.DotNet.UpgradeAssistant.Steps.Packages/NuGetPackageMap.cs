// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class NuGetPackageMap
    {
        public string PackageSetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the NetCore packages can be substituted while a project is still targeting .NET Framework.
        /// </summary>
        public bool NetCorePackagesWorkOnNetFx { get; set; }

        public IEnumerable<Reference> NetFrameworkAssemblies { get; set; } = Enumerable.Empty<Reference>();

        public IEnumerable<NuGetReference> NetFrameworkPackages { get; set; } = Enumerable.Empty<NuGetReference>();

        public IEnumerable<NuGetReference> NetCorePackages { get; set; } = Enumerable.Empty<NuGetReference>();

        public IEnumerable<Reference> NetCoreFrameworkReferences { get; set; } = Enumerable.Empty<Reference>();

        /// <summary>
        /// Determines whether a package map's .NET Framework assemblies include a
        /// given assembly reference.
        /// </summary>
        /// <param name="name">The assembly reference name to look for.</param>
        /// <returns>True if the reference exists in the map's .NET Framework assembly references. Otherwise, false.</returns>
        public bool ContainsAssemblyReference(string name) =>
            NetFrameworkAssemblies.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
