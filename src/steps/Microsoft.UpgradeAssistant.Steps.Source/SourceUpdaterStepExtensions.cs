using AspNetMigrator.SourceUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant;

namespace AspNetMigrator
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
