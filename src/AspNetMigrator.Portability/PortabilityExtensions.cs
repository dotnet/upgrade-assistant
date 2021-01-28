using System;
using AspNetMigrator.Portability;
using AspNetMigrator.Portability.Analyzers;
using AspNetMigrator.Portability.Service;
using AspNetMigrator.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class PortabilityExtensions
    {
        public static void AddPortabilityAnalysis(this IServiceCollection services)
        {
            services.AddTransient<IPortabilityAnalyzer, PortabilityServiceAnalyzer>();
            services.AddHttpClient<PortabilityService>(client =>
            {
                client.BaseAddress = new Uri("https://portability.dot.net");
            });
            services.AddSingleton<IPortabilityService>(sp => new MemoryCachingPortabilityService(sp.GetRequiredService<PortabilityService>()));
            services.AddTransient<IPageGenerator, PortabilityAnalysis>();
        }
    }
}
