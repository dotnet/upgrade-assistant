// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionOptions
    {
        public ICollection<string> ExtensionPaths { get; } = new List<string>();

        public IEnumerable<KeyValuePair<string, string>> AdditionalOptions { get; set; } = Enumerable.Empty<KeyValuePair<string, string>>();

        public Version CurrentVersion { get; set; } = null!;
    }
}
