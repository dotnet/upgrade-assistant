using AspNetMigrator.ConfigUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class ConfigUpdaterStepExtensions
    {
        public static OptionsBuilder<ConfigUpdaterStepOptions> AddConfigUpdaterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, ConfigUpdaterStep>();
            return services.AddOptions<ConfigUpdaterStepOptions>();
        }
    }
}
