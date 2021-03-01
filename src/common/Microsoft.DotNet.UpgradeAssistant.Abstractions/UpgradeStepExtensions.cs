// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgradeStepExtensions
    {
        /// <summary>
        /// Registers an upgrade step in the given service collection.
        /// </summary>
        /// <typeparam name="T">The type of upgrade step to register. Must derive from Microsoft.DotNet.UpgradeAssistant.UpgradeStep.</typeparam>
        /// <param name="services">The service collection to update.</param>
        /// <returns>The service collection, updated with type T registered as an upgrade service.</returns>
        public static IServiceCollection AddUpgradeStep<T>(this IServiceCollection services)
            where T : UpgradeStep =>
            services.AddTransient<UpgradeStep, T>();
    }
}
