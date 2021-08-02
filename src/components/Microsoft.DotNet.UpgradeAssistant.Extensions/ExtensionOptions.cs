// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionOptions
    {
        public string DefaultSource { get; set; } = string.Empty;

        public ICollection<string> DefaultExtensions { get; } = new List<string>();

        public ICollection<string> ExtensionPaths { get; } = new List<string>();

        public IEnumerable<AdditionalOption> AdditionalOptions { get; set; } = Enumerable.Empty<AdditionalOption>();

        public ICollection<ExtensionInstance> Extensions { get; } = new List<ExtensionInstance>();

        public bool IsDevelopment { get; set; } = true;

        public Version CurrentVersion { get; set; } = null!;

        public bool LoadExtensions { get; set; } = true;

        public string ConfigurationFilePath { get; } = Path.Combine(Environment.CurrentDirectory, "upgrade-assistant.json");

        public string ExtensionCachePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "DotNet Upgrade Assisistant", "extensions");
    }
}
