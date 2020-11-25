using AspNetMigrator.Engine;
using AspNetMigrator.MSBuild;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class MigratorMsBuildExtensions
    {
        public static void AddMsBuild(this IServiceCollection services)
        {
            services.AddSingleton<IVisualStudioFinder, VisualStudioFinder>();
            services.AddTransient<IPackageRestorer, MSBuildPackageRestorer>();
            services.AddTransient<IMigrationStartup, MSBuildRegistrationStartup>();

            // Instantiate the migration context with a func to avoid needing MSBuild types prior to MSBuild registration
            services.AddTransient<IMigrationContext>(sp => ActivatorUtilities.CreateInstance<MSBuildWorkspaceMigrationContext>(sp));
        }
    }
}
