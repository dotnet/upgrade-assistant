// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionCreator
    {
        bool TryCreateExtensionFromDirectory(IExtensionInstance extension, Stream stream);
    }
}
