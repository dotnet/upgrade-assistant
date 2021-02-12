using Microsoft.DotNet.UpgradeAssistant.Steps.Source;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Steps
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
