// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionDownloader
    {
        string GetInstallPath(ExtensionSource source);

        Task<string?> GetLatestVersionAsync(ExtensionSource n, CancellationToken token);

        Task<string?> RestoreAsync(ExtensionSource source, CancellationToken token);
    }
}
