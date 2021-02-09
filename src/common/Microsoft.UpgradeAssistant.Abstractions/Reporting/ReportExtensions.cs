using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant.Reporting;

namespace Microsoft.UpgradeAssistant
{
    public static class ReportExtensions
    {
        public static void AddReports(this IServiceCollection services)
        {
            services.AddScoped<IReportGenerator, ReportGenerator>();
        }
    }
}
