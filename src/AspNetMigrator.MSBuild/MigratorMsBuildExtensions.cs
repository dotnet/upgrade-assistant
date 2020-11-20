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
            services.AddTransient<IMigrationStartup, MSBuildRegistrationStartup>();
            services.AddTransient<IMigrationContext, MSBuildWorkspaceMigrationContext>();
        }
    }
}
