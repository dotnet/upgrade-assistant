using AspNetMigrator.TemplateUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant;

namespace AspNetMigrator
{
    public static class TemplateInserterStepExtensions
    {
        public static IServiceCollection AddTemplateInserterStep(this IServiceCollection services)
        {
            services.AddSingleton<TemplateProvider>();
            services.AddScoped<MigrationStep, TemplateInserterStep>();
            return services;
        }
    }
}
