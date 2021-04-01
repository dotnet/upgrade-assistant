// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public static class NuGetExtensions
    {
        public static NuGetVersion? GetNuGetVersion(this NuGetReference nugetRef)
        {
            if (nugetRef is null)
            {
                throw new System.ArgumentNullException(nameof(nugetRef));
            }

            return nugetRef.TryGetNuGetVersion(out var result) ? result : null;
        }

        public static bool TryGetNuGetVersion(this NuGetReference? nugetRef, [MaybeNullWhen(false)] out NuGetVersion result)
        {
            if (nugetRef is null)
            {
                result = null;
                return false;
            }

            if (nugetRef.HasWildcardVersion)
            {
                // https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution#floating-versions
                // reference versions can be any of the following (*, 4.*, 6.0.*)
                if (FloatRange.TryParse(nugetRef.Version, out var range))
                {
                    // when a range is used
                    if (range.HasMinVersion)
                    {
                        result = range.MinVersion;
                        return true;
                    }
                }
            }
            else if (NuGetVersion.TryParse(nugetRef.Version, out var version))
            {
                result = version;
                return true;
            }

            result = null;
            return false;
        }
    }
}
