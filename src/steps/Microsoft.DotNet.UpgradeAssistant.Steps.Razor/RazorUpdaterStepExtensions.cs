// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.DotNet.UpgradeAssistant.Steps.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class RazorUpdaterStepExtensions
    {
        public static IServiceCollection AddRazorUpdaterStep(this IServiceCollection services) =>
            services
            .AddUpgradeStep<RazorUpdaterStep>()
            .AddTransient<IUpdater<RazorCodeDocument>, RazorSourceUpdater>()
            .AddTransient<ITextMatcher, DefaultTextMatcher>();
    }
}
