using AspNetMigrator.ConfigUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class ConfigUpdaterStepExtensions
    {
        public static IServiceCollection AddConfigUpdaterStep(this IServiceCollection services) =>
            services.AddSingleton<ConfigUpdaterProvider>()
                .AddScoped<MigrationStep, ConfigUpdaterStep>();
    }
}
