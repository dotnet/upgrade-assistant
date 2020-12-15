using AspNetMigrator.TemplateUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class TemplateInserterStepExtensions
    {
        public static OptionsBuilder<TemplateInserterStepOptions> AddTemplateInserterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, TemplateInserterStep>();
            return services.AddOptions<TemplateInserterStepOptions>();
        }
    }
}
