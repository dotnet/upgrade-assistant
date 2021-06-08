﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionOptions
    {
        public ICollection<string> ExtensionPaths { get; } = new List<string>();
    }
}
