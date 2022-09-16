// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    internal static class PackageConfig
    {
        public static IEnumerable<NuGetReference> GetPackages(string path)
        {
            var doc = XDocument.Load(path);

            if (doc.Root is null)
            {
                yield break;
            }

            // Don't use doc.Root.Element because the
            // packages element likely *is* the root
            var packages = doc.Element("packages");

            if (packages is null)
            {
                yield break;
            }

            foreach (var package in packages.Descendants("package"))
            {
                var id = package.Attribute("id")?.Value;
                var version = package.Attribute("version")?.Value;

                if (id is not null && version is not null)
                {
                    yield return new NuGetReference(id, version);
                }
            }
        }
    }
}
