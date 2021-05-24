// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DiffPlex;
using DiffPlex.Chunkers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.DotNet.UpgradeAssistant.Steps.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Extension class with methods for registering RazorUpdaterStep-related services.
    /// </summary>
    public static class RazorUpdaterStepExtensions
    {
        /// <summary>
        /// Extension method for registering RazorUpdaterStep and related services.
        /// </summary>
        /// <param name="services">The IServiceCollection to register services in.</param>
        /// <returns>The services argument updated with RazorUpdaterStep and related services included.</returns>
        public static IServiceCollection AddRazorUpdaterStep(this IServiceCollection services) =>
            services
            .AddUpgradeStep<RazorUpdaterStep>()
            .AddTransient<IUpdater<RazorCodeDocument>, RazorSourceUpdater>()
            .AddTransient<IUpdater<RazorCodeDocument>, RazorHelperUpdater>()
            .AddTransient<ITextMatcher, DefaultTextMatcher>()
            .AddTransient<ITextReplacer, RazorTextReplacer>()
            .AddTransient<IDiffer, Differ>()
            .AddTransient<IChunker, CharacterChunker>();
    }
}
