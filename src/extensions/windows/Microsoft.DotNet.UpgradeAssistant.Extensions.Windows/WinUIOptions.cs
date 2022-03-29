// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WinUIOptions
    {
        public const string Name = "WinUIOptions";

#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, string>? NamespaceUpdates { get; set; }

        public WinUIOptionsProjectFilePropertyUpdates? ProjectFilePropertyUpdates { get; set; }

        public List<string>? FilesToDelete { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only
    }

    public class WinUIOptionsProjectFilePropertyUpdates
    {
        public Dictionary<string, string>? Set { get; set; }

        public List<string> Remove { get; set; }
    }
}
