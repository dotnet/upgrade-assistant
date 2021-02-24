// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
