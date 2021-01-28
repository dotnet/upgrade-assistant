using AspNetMigrator.PackageUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class PackageUpdaterStepExtensions
    {
        public static OptionsBuilder<PackageUpdaterOptions> AddPackageUpdaterStep(this IServiceCollection services)
        {
            services.AddSingleton<PackageMapProvider>();
            services.AddSingleton<ITargetFrameworkIdentifier, TargetFrameworkIdentifier>();
            services.AddScoped<MigrationStep, PackageUpdaterStep>();
            return services.AddOptions<PackageUpdaterOptions>();
        }
    }
}
