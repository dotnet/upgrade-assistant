using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ConfigurationUpdaterStepExtensions
    {
        public static IServiceCollection AddConfigUpdaterStep(this IServiceCollection services) =>
            services.AddScoped<MigrationStep, ConfigUpdaterStep>();
    }
}
