using System;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;

namespace AspNetMigrator
{
    public class TargetFrameworkMonikerComparer : ITargetFrameworkMonikerComparer
    {
        private readonly ILogger<TargetFrameworkMonikerComparer> _logger;

        public TargetFrameworkMonikerComparer(ILogger<TargetFrameworkMonikerComparer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsCompatible(TargetFrameworkMoniker tfm, TargetFrameworkMoniker other)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var dependent = NuGetFramework.Parse(tfm.Name);
            if (dependent.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", tfm.Name);
            }

            var dependency = NuGetFramework.Parse(other.Name);
            if (dependency.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", other.Name);
            }

            return DefaultCompatibilityProvider.Instance.IsCompatible(dependent, dependency);
        }
    }
}
