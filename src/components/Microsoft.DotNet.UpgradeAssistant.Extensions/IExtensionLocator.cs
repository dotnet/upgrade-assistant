// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionLocator
    {
        string GetInstallPath(ExtensionSource extensionSource);
    }
}
