using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool IsCoreCompatible(IEnumerable<TargetFrameworkMoniker> tfms)
        {
            return tfms
                .Select(tfm => NuGetFramework.Parse(tfm.Name))
                .Any(tfm => _provider.IsCompatible(_expected, tfm));
        }
    }
}
