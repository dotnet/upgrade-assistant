using AspNetMigrator.TryConvertUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class TryConvertProjectConverterStepExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddTryConvertProjectConverterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, TryConvertProjectConverterStep>();
            return services.AddOptions<TryConvertProjectConverterStepOptions>();
        }
    }
}
