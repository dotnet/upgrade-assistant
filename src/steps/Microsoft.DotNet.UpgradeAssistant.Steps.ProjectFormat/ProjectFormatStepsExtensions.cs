using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectFormatStepsExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddProjectFormatSteps(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, SetTFMStep>();
            services.AddScoped<MigrationStep, TryConvertProjectConverterStep>();
            services.AddTransient<ISectionGenerator, TryConvertReport>();

            return services.AddOptions<TryConvertProjectConverterStepOptions>();
        }
    }
}
