// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record NuGetPackageMetadata
    {
        /// <summary>
        /// Gets the package owners stored in the Nuspec file.
        /// </summary>
        public string? Owners { get; init; }
    }
}
