﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IPackageCreator
    {
        bool CreateArchive(IExtensionInstance extension, string? packageType, Stream stream);
    }
}
