// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class AnalysisExtensions
    {
        public static void AddAnalysis(this IServiceCollection services, Action<AnalysisOptions> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<AnalysisOptions>()
                .Configure(configure)
                .ValidateDataAnnotations();
            services.AddTransient<IAnalyzeResultWriterProvider, AnalyzerResultProviderWriter>();
            services.AddTransient<IAnalyzeResultWriter, HtmlAnalyzeResultWriter>();
            services.AddTransient<IAnalyzeResultWriter, SarifAnalyzeResultWriter>();
        }
    }
}
