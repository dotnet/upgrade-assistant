// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Steps.Solution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class SolutionStepsExtensions
    {
        public static void AddSolutionSteps(this IServiceCollection services)
        {
            services.AddSingleton<IEntrypointResolver, EntrypointResolver>();

            services.AddUpgradeStep<CurrentProjectSelectionStep>();
            services.AddUpgradeStep<NextProjectStep>();
            services.AddUpgradeStep<FinalizeSolutionStep>();
            services.AddUpgradeStep<EntrypointSelectionStep>();
        }
    }
}
