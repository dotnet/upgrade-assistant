﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client
{
    /// <summary>
    /// A NuGet package id and version pair.
    /// </summary>
    public record NuGetPackageVersion(string Id, string Version);
}
