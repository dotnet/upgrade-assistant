// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public record ExtensionServiceConfiguration(IServiceCollection Services, IConfiguration Configuration) : IExtensionServiceCollection
    {
        public IExtensionOptionsBuilder<TOption> AddExtensionOption<TOption>(string sectionName)
            where TOption : class, new()
        {
            Services.AddTransient<IConfigureOptions<TOption>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IEnumerable<IExtension>>();

                return new AggregateExtensionConfigureOptions<TOption>(sectionName, extensions);
            });

            Services.AddTransient<IConfigureOptions<OptionCollection<TOption>>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IEnumerable<IExtension>>();

                return new AggregateExtensionConfigureOptions<TOption>(sectionName, extensions);
            });

            Services.AddTransient<IConfigureOptions<OptionCollection<FileOption<TOption>>>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IEnumerable<IExtension>>();

                return new AggregateExtensionConfigureOptions<TOption>(sectionName, extensions);
            });

            return new ExtensionOptionsBuilder<TOption>(this);
        }
    }
}
