// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public record ProgramFileSpec
    {
        public string Path { get; set; } = string.Empty;

        public bool IsDpiSettingSet { get; set; }

        public string[] FileContent { get; set; } = Array.Empty<string>();

        public ProgramFileSpec()
        {
        }

        public ProgramFileSpec(string path)
        {
            Path = path;
            if (Path is not null && File.Exists(Path))
            {
                FileContent = File.ReadAllLines(Path);

                IsDpiSettingSet = FileContent.Where(x => x.Contains("Application.SetHighDpiMode")).FirstOrDefault() is not null;
            }
        }
    }
}
