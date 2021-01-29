using AspNetMigrator.Portability;
using AspNetMigrator.Portability.Analyzers;
using AspNetMigrator.Portability.Service;
using AspNetMigrator.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetMigrator
{
    public static class PortabilityExtensions
    {
        public static OptionsBuilder<PortabilityOptions> AddPortabilityAnalysis(this IServiceCollection services)
        {
            services.AddTransient<IPortabilityAnalyzer, PortabilityServiceAnalyzer>();
            services.AddHttpClient<PortabilityService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<PortabilityOptions>>();
                client.BaseAddress = options.Value.ServiceEndpoint;
            });
            services.AddSingleton<IPortabilityService>(sp => new MemoryCachingPortabilityService(sp.GetRequiredService<PortabilityService>()));
            services.AddTransient<ISectionGenerator, PortabilityAnalysis>();

            return services.AddOptions<PortabilityOptions>()
                .ValidateDataAnnotations();
        }
    }
}
