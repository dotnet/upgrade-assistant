using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
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

            var packages = doc.Root.Element("packages");

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
