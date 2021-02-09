using AspNetMigrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant.Steps.Configuration;

namespace Microsoft.UpgradeAssistant.Steps
{
    public static class ConfigUpdaterStepExtensions
    {
        public static IServiceCollection AddConfigUpdaterStep(this IServiceCollection services)
        {
            services.AddSingleton<ConfigUpdaterProvider>();
            services.AddScoped<MigrationStep, ConfigUpdaterStep>();
            return services;
        }
    }
}
