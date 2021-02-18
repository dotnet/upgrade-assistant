using System;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class MigrateOptions
    {
        public FileInfo Project { get; set; } = null!;

        public string ProjectPath => Project.FullName;

        public string[] Extension { get; set; } = Array.Empty<string>();

        public bool SkipBackup { get; set; }

        public bool Verbose { get; set; }

        public bool NonInteractive { get; set; }

        public int NonInteractiveWait { get; set; } = 2;

        public UpgradeTarget UpgradeTarget { get; set; } = UpgradeTarget.Current;
    }
}
