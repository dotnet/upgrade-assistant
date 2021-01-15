using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Frameworks;

namespace AspNetMigrator
{
    public class TargetFrameworkIdentifier : ITargetFrameworkIdentifier
    {
        private readonly IFrameworkCompatibilityProvider _provider;
        private readonly NuGetFramework _expected;

        public TargetFrameworkIdentifier(MigrateOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _provider = DefaultCompatibilityProvider.Instance;
            _expected = NuGetFramework.Parse(options.TargetFramework);
        }

        public bool IsCoreCompatible(Stream projectFile)
        {
            var doc = XDocument.Load(projectFile);

            var targetFrameworkNode = doc.Descendants("TargetFramework");
            var targetFrameworksNode = doc.Descendants("TargetFrameworks");
            var all = targetFrameworksNode.Concat(targetFrameworkNode);

            return all
                .SelectMany(Parse)
                .Any(tfm => _provider.IsCompatible(_expected, tfm));
        }

        private static IEnumerable<NuGetFramework> Parse(XElement element)
        {
            if (element is null)
            {
                yield break;
            }

            var tfms = element.Value.Split(';');

            foreach (var tfm in tfms)
            {
                yield return NuGetFramework.Parse(tfm);
            }
        }
    }
}
