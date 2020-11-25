using System.IO;

namespace AspNetMigrator.Engine
{
    public class MigrateOptions
    {
        public FileInfo Project { get; set; } = null!;

        public string ProjectPath => Project.FullName;

        public string? BackupPath { get; set; }

        public bool SkipBackup { get; set; }

        public bool Verbose { get; set; }

        // TODO: Allow the user to specify Current or LTS and initialize this with an appropriate string from configuration based on the user's specification
        public string TargetFramework { get; set; } = "net5.0";

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProjectPath) && File.Exists(ProjectPath);
    }
}
