// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class NuGetTargetFrameworkMonikerComparer : ITargetFrameworkMonikerComparer
    {
        private readonly ILogger<NuGetTargetFrameworkMonikerComparer> _logger;

        public NuGetTargetFrameworkMonikerComparer(ILogger<NuGetTargetFrameworkMonikerComparer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Compare(TargetFrameworkMoniker? x, TargetFrameworkMoniker? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var dependent = NuGetFramework.Parse(x.Name);
            if (dependent.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", x.Name);
            }

            var dependency = NuGetFramework.Parse(y.Name);
            if (dependency.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", y.Name);
            }

            if (y.IsNetStandard && x.IsFramework)
            {
                return -1;
            }

            return Compare(dependent, dependency);
        }

        private static int Compare(NuGetFramework dependent, NuGetFramework dependency)
        {
            if (dependent.Equals(dependency))
            {
                return 0;
            }

            var dependentToDependency = DefaultCompatibilityProvider.Instance.IsCompatible(dependent, dependency);
            var dependencyToDependent = DefaultCompatibilityProvider.Instance.IsCompatible(dependency, dependent);

            return (dependentToDependency, dependencyToDependent) switch
            {
                (true, true) => 0,
                (false, true) => -1,
                (true, false) => 1,
                (false, false) => -1,
            };
        }

        public TargetFrameworkMoniker Merge(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
        {
            if (tfm1 is null)
            {
                throw new ArgumentNullException(nameof(tfm1));
            }

            if (tfm2 is null)
            {
                throw new ArgumentNullException(nameof(tfm2));
            }

            var nugetTfm1 = NuGetFramework.Parse(tfm1.Name);
            var nugetTfm2 = NuGetFramework.Parse(tfm2.Name);

            // We can only combine if the platform is the same or at least one is null.
            if (!IsPlatformSameOrNull(nugetTfm1, nugetTfm2))
            {
                throw new InvalidOperationException("Cannot merge two target frameworks with different platforms");
            }

            var platform = GetPlatform(nugetTfm1, nugetTfm2);

            // We need to compare without the platform and we'll handle that later.
            var noPlatform1 = new NuGetFramework(nugetTfm1.Framework, nugetTfm1.Version);
            var noPlatform2 = new NuGetFramework(nugetTfm2.Framework, nugetTfm2.Version);

            var maxFramework = Compare(noPlatform1, noPlatform2) > 0
                ? nugetTfm1 : nugetTfm2;
            var version = (nugetTfm1.PlatformVersion, nugetTfm2.PlatformVersion) switch
            {
                (Version v1, null) => v1,
                (null, Version v2) => v2,
                (Version v1, Version v2) when v1 >= v2 => v1,
                (Version v1, Version v2) when v2 > v1 => v2,
                _ => null,
            };

            var newFramework = (platform, version) switch
            {
                (string p, Version v) => new NuGetFramework(maxFramework.Framework, maxFramework.Version, p, v),
                (string p, null) => new NuGetFramework(maxFramework.Framework, maxFramework.Version, p, new Version(0, 0, 0, 0)),
                _ => new NuGetFramework(maxFramework.Framework, maxFramework.Version),
            };

            return new TargetFrameworkMoniker(newFramework.GetShortFolderName());

            static string? GetPlatform(NuGetFramework f1, NuGetFramework f2)
            {
                if (f1.HasPlatform)
                {
                    return f1.Platform;
                }
                else if (f2.HasPlatform)
                {
                    return f2.Platform;
                }
                else
                {
                    return null;
                }
            }

            static bool IsPlatformSameOrNull(NuGetFramework f1, NuGetFramework f2)
            {
                if (f1.Platform == f2.Platform)
                {
                    return true;
                }

                if (!f1.HasPlatform || !f2.HasPlatform)
                {
                    return true;
                }

                return false;
            }
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

            return Compare(tfm, other) >= 0;
        }
    }
}
