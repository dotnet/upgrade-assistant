using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UpgradeAssistant;
using Microsoft.UpgradeAssistant.Reporting;

namespace Microsoft.UpgradeAssistant.Steps.ProjectFormat
{
    public static class TryConvertProjectConverterStepExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddTryConvertProjectConverterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, TryConvertProjectConverterStep>();
            services.AddTransient<ISectionGenerator, TryConvertReport>();

            return services.AddOptions<TryConvertProjectConverterStepOptions>();
        }
    }
}
