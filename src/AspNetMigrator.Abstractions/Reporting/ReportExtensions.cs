using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class ReportExtensions
    {
        public static void AddReports(this IServiceCollection services)
        {
            services.AddScoped<IReportGenerator, ReportGenerator>();
            services.AddScoped<IPageGenerator, TestPageGenerator>();
        }

        private class TestPageGenerator : IPageGenerator
        {
            public async IAsyncEnumerable<Page> GeneratePages(IMigrationContext context, [EnumeratorCancellation] CancellationToken token)
            {
                await Task.Yield();

                yield return new Page("Test")
                {
                    Content = new Content[]
                    {
                        new Text("Test content for mocking reports"),
                    }
                };
            }
        }
    }
}
