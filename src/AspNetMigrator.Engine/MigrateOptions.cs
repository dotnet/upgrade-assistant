using System.IO;

namespace AspNetMigrator.Engine
{
    public class MigrateOptions
    {
        public FileInfo Project { get; set; }

        public string ProjectPath => Project.FullName;

        public string BackupPath { get; set; }

        public bool SkipBackup { get; set; }

        public bool Verbose { get; set; }

        public bool IsValid() => !string.IsNullOrWhiteSpace(ProjectPath) && File.Exists(ProjectPath);
    }
}
