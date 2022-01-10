﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record NuGetPackageMetadata
    {
        /// <summary>
        /// Gets the package owners stored in the Nuspec file.
        /// </summary>
        public string? Owners { get; init; }

        public ImmutableArray<NuGetDependencyInformation> Dependencies { get; init; } = ImmutableArray<NuGetDependencyInformation>.Empty;
    }
}
