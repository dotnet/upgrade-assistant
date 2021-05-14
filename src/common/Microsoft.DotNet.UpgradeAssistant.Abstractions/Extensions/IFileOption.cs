// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IFileOption
    {
        IFileProvider Files { get; set; }
    }
}
