// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    /// <summary>
    /// An abstraction that allows managing extensions used in the system.
    /// </summary>
    public interface IExtensionManager
    {
        Task<bool> RemoveAsync(string name, CancellationToken token);

        Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token);

        Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token);

        Task<bool> RestoreExtensionsAsync(CancellationToken token);

        IAsyncEnumerable<ExtensionSource> SearchAsync(string query, string source, CancellationToken token);
    }
}
