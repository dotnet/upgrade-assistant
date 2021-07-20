// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionOptions
    {
        public ICollection<string> DefaultExtensions { get; } = new List<string>();

        public ICollection<string> ExtensionPaths { get; } = new List<string>();

        public IEnumerable<AdditionalOption> AdditionalOptions { get; set; } = Enumerable.Empty<AdditionalOption>();

        public ICollection<ExtensionInstance> Extensions { get; } = new List<ExtensionInstance>();

        public Version CurrentVersion { get; set; } = null!;

        public IFileProvider CachedExtensions { get; } = new PhysicalFileProvider(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Upgrade Assistant"));

        public string DataFile { get; } = Path.Combine(Environment.CurrentDirectory, "upgrade-assistant.json");
    }
}
