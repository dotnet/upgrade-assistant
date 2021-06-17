// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record PackageSearchOptions
    {
        /// <summary>
        /// Gets a value indicating whether to search for prerelease versions.
        /// </summary>
        public bool Prerelease { get; init; }

        /// <summary>
        /// Gets a value indicating whether to search for unlisted packages.
        /// </summary>
        public bool Unlisted { get; init; }

        /// <summary>
        /// Gets a value indicating whether to search for latest changes within the same major version when supplied.
        /// </summary>
        public bool LatestMinorAndBuildOnly { get; init; }
    }
}
