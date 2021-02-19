// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public static class SolutionLevelExtensions
    {
        public static void AddSolutionLevelSteps(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, CurrentProjectSelectionStep>();
            services.AddScoped<MigrationStep, EntrypointSelectionStep>();
        }
    }
}
