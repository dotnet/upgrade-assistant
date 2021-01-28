using AspNetMigrator.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class ReportExtensions
    {
        public static void AddReports(this IServiceCollection services)
        {
            services.AddScoped<IReportGenerator, ReportGenerator>();
        }
    }
}
