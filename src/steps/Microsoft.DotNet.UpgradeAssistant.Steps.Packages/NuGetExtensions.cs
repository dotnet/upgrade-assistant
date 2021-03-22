// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    internal static class NuGetExtensions
    {
        public static NuGetVersion? GetNuGetVersion(this NuGetReference nugetRef)
        {
            if (nugetRef.HasWildcardVersion)
            {
                if (FloatRange.TryParse(nugetRef.Version, out var range))
                {
                    if (range.HasMinVersion)
                    {
                        return range.MinVersion;
                    }
                }

                return null;
            }

            if (NuGetVersion.TryParse(nugetRef.Version, out var version))
            {
                return version;
            }

            return null;
        }
    }
}
