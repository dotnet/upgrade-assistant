// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public interface INuGetPackageSourceFactory
    {
        IEnumerable<PackageSource> GetPackageSources(string? path);
    }
}
