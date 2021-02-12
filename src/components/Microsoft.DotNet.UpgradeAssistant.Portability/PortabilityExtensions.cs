using Microsoft.DotNet.UpgradeAssistant.Portability;
using Microsoft.DotNet.UpgradeAssistant.Portability.Analyzers;
using Microsoft.DotNet.UpgradeAssistant.Portability.Service;
using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
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
