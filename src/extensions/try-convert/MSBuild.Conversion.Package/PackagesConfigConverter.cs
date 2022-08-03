// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace MSBuild.Conversion.Package
{
    public static class PackagesConfigConverter
    {
        /// <summary>
        /// Given a path to a 'packages.config' file, gets an enumerable of package reference items.
        /// </summary>
        /// <param name="path">The path on disk to a specific packages.config file.</param>
        public static IEnumerable<PackageReferencePackage> Convert(string path) =>
            PackagesConfigParser.Parse(path).Select(pkg => new PackageReferencePackage(pkg));
    }
}
