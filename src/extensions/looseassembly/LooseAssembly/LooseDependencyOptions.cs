// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    public class LooseDependencyOptions : IFileOption
    {
        public string[] Indexes { get; set; } = Array.Empty<string>();

        public IFileProvider Files { get; set; } = null!;
    }
}
