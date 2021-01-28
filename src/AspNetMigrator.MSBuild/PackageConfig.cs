using System.Collections.Generic;
using System.Xml.Linq;

namespace AspNetMigrator.MSBuild
{
    internal static class PackageConfig
    {
        public static IEnumerable<NuGetReference> GetPackages(string path)
        {
            var _doc = XDocument.Load(path);

            if (_doc.Root is null)
            {
                yield break;
            }

            var packages = _doc.Root.Element("packages");

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
