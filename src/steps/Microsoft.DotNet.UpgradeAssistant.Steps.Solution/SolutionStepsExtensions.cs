// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Solution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class SolutionStepsExtensions
    {
        public static void AddSolutionSteps(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddSingleton<IEntrypointResolver, EntrypointResolver>();

            services.Services.AddUpgradeStep<CurrentProjectSelectionStep>();
            services.Services.AddUpgradeStep<NextProjectStep>();
            services.Services.AddUpgradeStep<FinalizeSolutionStep>();
            services.Services.AddUpgradeStep<EntrypointSelectionStep>();

            services.AddExtensionOption<SolutionOptions>("Solution");
        }
    }
}
