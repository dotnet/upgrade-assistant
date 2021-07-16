// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionManager
    {
        IEnumerable<ExtensionInstance> Instances { get; }

        IEnumerable<ExtensionSource> Registered { get; }

        bool Remove(string name);

        Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token);

        Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token);
    }
}
