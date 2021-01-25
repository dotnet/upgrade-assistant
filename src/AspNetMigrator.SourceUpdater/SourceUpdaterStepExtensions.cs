﻿using AspNetMigrator.SourceUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class SourceUpdaterStepExtensions
    {
        public static IServiceCollection AddSourceUpdaterStep(this IServiceCollection services) =>
            services.AddSingleton<AnalyzerProvider>()
                .AddScoped<MigrationStep, SourceUpdaterStep>();
    }
}
