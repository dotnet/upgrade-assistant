// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageSearch
    {
        IAsyncEnumerable<NuGetReference> SearchAsync(string name, string source, string? packageType, CancellationToken token);
    }
}
