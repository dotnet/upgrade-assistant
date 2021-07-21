// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionManager
    {
        IEnumerable<IExtensionInstance> Instances { get; }

        IEnumerable<ExtensionSource> Registered { get; }

        bool Remove(string name);

        Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token);

        Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token);

        /// <summary>
        /// Attempts to find the extension in which a service is registered.
        /// </summary>
        /// <param name="service">Service to search for.</param>
        /// <param name="extensionInstance">An extension if it is defined in one.</param>
        /// <returns><c>true</c> if an extension is found; otherwise <c>false</c>.</returns>
        bool TryGetExtension(object service, [MaybeNullWhen(false)] out IExtensionInstance extensionInstance);
    }
}
