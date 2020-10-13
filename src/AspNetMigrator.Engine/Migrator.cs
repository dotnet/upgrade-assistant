using System;
using System.IO;
using System.Threading.Tasks;
using AspNetMigrator.MSBuild;

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
            ProjectConverter = projectConverter ?? throw new ArgumentNullException(nameof(projectConverter));
            PackageUpdater = packageUpdater ?? throw new ArgumentNullException(nameof(packageUpdater));
            SourceUpdater = sourceUpdater ?? throw new ArgumentNullException(nameof(sourceUpdater));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> MigrateAsync(string projectPath, string backupPath)
        {
            if (!File.Exists(projectPath))
            {
                Logger.Fatal("Project file does not exist: {ProjectPath}", projectPath);
                return false;
            }

            if (!Path.GetExtension(projectPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Fatal("Project file ({ProjectPath}) is not a C# project", projectPath);
                return false;
            }

            // Backup
            var projectName = Path.GetFileName(projectPath);
            Logger.Information("Beginning migration of {ProjectName} (backup path: [{BackupPath}]", projectName, backupPath ?? "N/A");

            if (backupPath != null)
            {
                if (CreateBackup(Path.GetDirectoryName(projectPath), backupPath))
                {
                    Logger.Information("Backup successfully created");
                }
                else
                {
                    Logger.Fatal("Backup failed");
                    return false;
                }
            }
            else
            {
                Logger.Information("Skipping backup");
            }

            // Convert to SDK-style project
            Logger.Information("Converting csproj to SDK style");
            if (await ProjectConverter.ConvertAsync(projectPath))
            {
                Logger.Information("Project file conversion complete");
            }
            else
            {
                Logger.Fatal("Failed to convert project file to SDK style");
                return false;
            }

            // Register correct MSBuild for use with SDK-style projects
            MSBuildHelper.RegisterMSBuildInstance();

            // Update NuGet packages to Core-compatible versions
            Logger.Information("Updating NuGet references");
            if (await PackageUpdater.UpdatePackagesAsync(projectPath))
            {
                Logger.Information("NuGet packages updated");
            }
            else
            {
                Logger.Fatal("Failed to update NuGet packages");
                return false;
            }

            // Apply source level code fixes
            Logger.Information("Updating project source");
            if (await SourceUpdater.UpdateSourceAsync(projectPath))
            {
                Logger.Information("Source updated");
            }
            else
            {
                Logger.Warning("Failed to update project source. Check that NuGet packages restore properly and re-run this tool.");
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
