using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant;
using Microsoft.UpgradeAssistant.Steps.Source;

namespace Microsoft.UpgradeAssistant.Steps
{
    public static class SourceUpdaterStepExtensions
    {
        public static IServiceCollection AddSourceUpdaterStep(this IServiceCollection services)
        {
            services.AddSingleton<AnalyzerProvider>();
            services.AddScoped<MigrationStep, SourceUpdaterStep>();
            return services;
        }
    }
}
