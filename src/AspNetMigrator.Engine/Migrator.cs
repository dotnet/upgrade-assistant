using System.IO;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class Migrator
    {
        private IProjectConverter ProjectConverter { get; }
        private IPackageUpdater PackageUpdater { get; }
        private ISourceUpdater SourceUpdater { get; }
        private ILogger Logger { get; }


        public Migrator(IProjectConverter projectConverter, IPackageUpdater packageUpdater, ISourceUpdater sourceUpdater, ILogger logger)
        {
            ProjectConverter = projectConverter ?? throw new System.ArgumentNullException(nameof(projectConverter));
            PackageUpdater = packageUpdater ?? throw new System.ArgumentNullException(nameof(packageUpdater));
            SourceUpdater = sourceUpdater ?? throw new System.ArgumentNullException(nameof(sourceUpdater));
            Logger = logger;
        }

        public async Task<bool> MigrateAsync(string projectPath, string backupPath)
        {
            if (!File.Exists(projectPath))
            {
                Logger.Fatal("Project file does not exist: {ProjectPath}", projectPath);
                return false;
            }

            var projectName = Path.GetFileName(projectPath);
            Logger.Information("Beginning migration of {ProjectName} (backup path: [{BackupPath}]", projectName, backupPath ?? "N/A");

            if (backupPath != null)
            {
                if (!CreateBackup(Path.GetDirectoryName(projectPath), backupPath))
                {
                    Logger.Fatal("Backup failed");
                    return false;
                }
                else
                {
                    Logger.Information("Backup successfully created");
                }
            }
            else
            {
                Logger.Information("Skipping backup");
            }

            Logger.Information("Migration of {ProjectName} complete", projectName);

            return true;
        }

        private bool CreateBackup(string projectDir, string backupPath)
        {
            Logger.Information("Backing up {ProjectPath} to {BackupPath}", projectDir, backupPath);
            try
            {
                Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(backupPath))
                {
                    Logger.Error("Backup directory ({BackupPath}) not created", backupPath);
                    return false;
                }

                CopyDirectory(projectDir, backupPath);
                Logger.Information("Project backed up to {BackupPath}", backupPath);
                return true;
            }
            catch (IOException exc)
            {
                Logger.Error("Unexpected exception while creating backup: {Exception}", exc);
                return false;
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            var directoryInfo = new DirectoryInfo(sourceDir);
            foreach (var file in directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var dest = Path.Combine(destinationDir, file.Name);
                File.Copy(file.FullName, dest, true);
                Logger.Verbose("Copied {SourceFile} to {DestinationFile}", file.FullName, dest);
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                CopyDirectory(dir.FullName, Path.Combine(destinationDir, dir.Name));
            }
        }
    }
}
