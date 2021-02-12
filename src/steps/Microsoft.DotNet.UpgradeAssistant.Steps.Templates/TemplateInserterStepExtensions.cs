using Microsoft.DotNet.UpgradeAssistant.Steps.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Steps
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
