using AspNetMigrator.PackageUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class PackageUpdaterStepExtensions
    {
        public static OptionsBuilder<PackageUpdaterStepOptions> AddPackageUpdaterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, PackageUpdaterStep>();
            return services.AddOptions<PackageUpdaterStepOptions>();
        }
    }
}
