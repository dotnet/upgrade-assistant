// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class NuGetVersionComparer : IVersionComparer
    {
        public int Compare(string? x, string? y)
        {
            if (NuGetVersion.TryParse(x, out var nx))
            {
                return -1;
            }

            if (NuGetVersion.TryParse(y, out var ny))
            {
                return 1;
            }

            return nx.CompareTo(ny);
        }

        public int Compare(NuGetReference? x, NuGetReference? y)
        {
            if (!x.TryGetNuGetVersion(out var nx))
            {
                return -1;
            }

            if (!y.TryGetNuGetVersion(out var ny))
            {
                return 1;
            }

            return nx.CompareTo(ny);
        }

        public bool IsMajorChange(NuGetReference x, NuGetReference y)
        {
            var nx = x.GetNuGetVersion();
            var ny = y.GetNuGetVersion();

            return nx?.Major != ny?.Major;
        }
    }
}
