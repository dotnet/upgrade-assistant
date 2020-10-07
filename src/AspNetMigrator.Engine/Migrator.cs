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

        public async Task MigrateAsync(string projectPath, string backupPath)
        {

        }
    }
}
