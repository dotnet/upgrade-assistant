using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant.Steps.Templates;

namespace Microsoft.UpgradeAssistant.Steps
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
