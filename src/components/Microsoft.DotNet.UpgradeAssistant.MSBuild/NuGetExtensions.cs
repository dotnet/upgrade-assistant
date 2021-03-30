// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal static class NuGetExtensions
    {
        public static NuGetVersion? GetNuGetVersion(this NuGetReference nugetRef)
        {
            if (nugetRef.HasWildcardVersion)
            {
                return null;
            }

            return NuGetVersion.Parse(nugetRef.Version);
        }
    }
}
