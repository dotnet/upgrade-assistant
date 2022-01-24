// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetVersionComparer : IVersionComparer
    {
        public bool TryFindBestVersion(IEnumerable<string> versions, [MaybeNullWhen(false)] out string bestMatch)
        {
            if (versions is null)
            {
                throw new System.ArgumentNullException(nameof(versions));
            }

            var nugetVersions = new List<NuGetVersion>();
            var ranges = new List<VersionRange>();

            foreach (var v in versions)
            {
                if (NuGetVersion.TryParse(v, out var parsedVersion))
                {
                    nugetVersions.Add(parsedVersion);
                }

                if (VersionRange.TryParse(v, out var range))
                {
                    ranges.Add(range);
                }
            }

            if (nugetVersions.Count == 0 && ranges.Count == 0)
            {
                bestMatch = null;
                return false;
            }

            var finalRange = VersionRange.CommonSubSet(ranges);

            if (nugetVersions.Count > 0)
            {
                bestMatch = finalRange.FindBestMatch(nugetVersions).ToNormalizedString();
                return true;
            }
            else
            {
                bestMatch = finalRange.MinVersion.ToNormalizedString();
                return true;
            }
        }

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
