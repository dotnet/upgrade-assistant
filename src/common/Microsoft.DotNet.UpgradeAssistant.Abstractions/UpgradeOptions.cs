// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeOptions
    {
        public FileInfo Project { get; set; } = null!;

        public string ProjectPath => Project.FullName;

        // Name must be Extension and not plural as the name of the argument that it binds to is `--extension`
        public IReadOnlyCollection<string> Extension { get; set; } = Array.Empty<string>();

        public bool SkipBackup { get; set; }

        // Name must be EntryPoint and not plural as the name of the argument that it binds to is `--entry-point`
        public IReadOnlyCollection<string> EntryPoint { get; set; } = Array.Empty<string>();

        public bool Verbose { get; set; }

        public bool NonInteractive { get; set; }

        public int NonInteractiveWait { get; set; } = 2;

        public UpgradeTarget UpgradeTarget { get; set; } = UpgradeTarget.Current;
    }
}
