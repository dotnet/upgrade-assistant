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
                // https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
                // reference versions can be any of the following (*, 4.*, 6.0.*)
                if (FloatRange.TryParse(nugetRef.Version, out var range))
                {
                    // when a range is used
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
