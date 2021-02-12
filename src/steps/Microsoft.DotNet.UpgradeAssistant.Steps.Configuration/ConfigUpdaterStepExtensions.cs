using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Steps
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
