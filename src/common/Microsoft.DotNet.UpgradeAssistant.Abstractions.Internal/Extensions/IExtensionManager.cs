// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionManager
    {
        /// <summary>
        /// Gets the extensions that are currently loaded. This can only be retrieved after extensions have been loaded.
        /// </summary>
        IEnumerable<IExtensionInstance> Instances { get; }

        IEnumerable<ExtensionSource> Registered { get; }

        Task<bool> RemoveAsync(string name, CancellationToken token);

        Task<ExtensionSource?> UpdateAsync(string name, CancellationToken token);

        Task<ExtensionSource?> AddAsync(ExtensionSource n, CancellationToken token);

        Task<bool> RestoreExtensionsAsync(CancellationToken token);

        IExtensionInstance? LoadExtension(string path);

        bool CreateExtensionFromDirectory(IExtensionInstance extension, Stream stream);

        /// <summary>
        /// Attempts to find the extension in which a service is registered.
        /// </summary>
        /// <param name="service">Service to search for.</param>
        /// <param name="extensionInstance">An extension if it is defined in one.</param>
        /// <returns><c>true</c> if an extension is found; otherwise <c>false</c>.</returns>
        bool TryGetExtension(object service, [MaybeNullWhen(false)] out IExtensionInstance extensionInstance);
    }
}
