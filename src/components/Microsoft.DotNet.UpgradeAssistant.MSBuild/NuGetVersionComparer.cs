// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class NuGetVersionComparer : IVersionComparer
    {
        public int Compare(string? x, string? y)
        {
            if (!NuGetVersion.TryParse(x, out var nx))
            {
                return -1;
            }

            if (!NuGetVersion.TryParse(y, out var ny))
            {
                return 1;
            }

            return nx.CompareTo(ny);
        }

        public bool IsMajorChange(string x, string y)
        {
            if (!TryGetMajorValue(x, out var xMajor))
            {
                return false;
            }

            if (!TryGetMajorValue(y, out var yMajor))
            {
                return false;
            }

            return xMajor != yMajor;
        }

        private static bool TryGetMajorValue(string version, out int majorVersion)
        {
            if (NuGetVersion.TryParse(version, out var nversion))
            {
                majorVersion = nversion.Major;
                return true;
            }

            if (FloatRange.TryParse(version, out var fversion) && fversion.HasMinVersion)
            {
                majorVersion = fversion.MinVersion.Major;
                return true;
            }

            majorVersion = default;
            return false;
        }
    }
}
