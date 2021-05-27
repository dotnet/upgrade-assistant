// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal record ExtensionServiceCollection(IServiceCollection Services, ExtensionInstance Extension) : IExtensionServiceCollection
    {
        public IConfiguration Configuration => Extension.Configuration;

        public IFileProvider Files => Extension.FileProvider;

        public IExtensionOptionsBuilder<TOption> AddExtensionOption<TOption>(string sectionName)
            where TOption : class, new()
        {
            // Register options first so our custom factory isn't overwritten
            Services.AddOptions();

            // The default options factory cannot create an instance of ICollection<>
            Services.AddTransient<IOptionsFactory<ICollection<TOption>>, CollectionOptionsFactory<TOption>>();

            Services.AddTransient<IConfigureOptions<TOption>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IEnumerable<ExtensionInstance>>();

                return new AggregateExtensionConfigureOptions<TOption>(sectionName, extensions);
            });

            Services.AddTransient<IConfigureOptions<ICollection<TOption>>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IEnumerable<ExtensionInstance>>();

                return new AggregateExtensionConfigureOptions<TOption>(sectionName, extensions);
            });

            return new ExtensionOptionsBuilder<TOption>(this, sectionName);
        }
    }
}
