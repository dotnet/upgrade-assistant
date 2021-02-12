using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ReportExtensions
    {
        public static void AddReports(this IServiceCollection services)
        {
            services.AddScoped<IReportGenerator, ReportGenerator>();
        }
    }
}
