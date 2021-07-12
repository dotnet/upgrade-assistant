// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionOptionsBuilder<TOption> : IExtensionOptionsBuilder<TOption>
        where TOption : class, new()
    {
        private readonly ExtensionServiceCollection _services;
        private readonly string _sectionName;

        public ExtensionOptionsBuilder(ExtensionServiceCollection services, string sectionName)
        {
            _services = services;
            _sectionName = sectionName;
        }

        public void MapFiles<TTo>(Func<TOption, string?> factory)
            => MapFiles<TTo>(o =>
            {
                var result = factory(o);

                return result is null ? Enumerable.Empty<string>() : new[] { result };
            });

        private delegate ExtensionMappedFileConfigureOptions<TOption, TTo> ExtensionMappedConfigurationFactory<TTo>(Func<TOption, IEnumerable<string>> factory);

        public void MapFiles<TTo>(Func<TOption, IEnumerable<string>> factory)
        {
            // The default options factory cannot create an instance of ICollection<>
            _services.Services.AddTransient<IOptionsFactory<ICollection<TTo>>, CollectionOptionsFactory<TTo>>();
            _services.Services.AddTransient<IOptionsFactory<ICollection<FileOption<TOption>>>, CollectionOptionsFactory<FileOption<TOption>>>();

            // Since not all TOption instances will implement IFileOption, this is to ensure we are able to access an IFileProvider from the extension
            _services.Services.AddTransient<IConfigureOptions<ICollection<FileOption<TOption>>>>(ctx =>
            {
                var extensions = ctx.GetRequiredService<IExtensionManager>();

                return new AggregateExtensionConfigureOptions<TOption>(_sectionName, extensions);
            });

            _services.Services.TryAddTransient(typeof(ExtensionMappedFileConfigureOptions<,>));

            _services.Services.AddTransient<IConfigureOptions<ICollection<TTo>>>(ctx =>
            {
                var configureOptionsFactory = ctx.GetRequiredService<ExtensionMappedConfigurationFactory<TTo>>();

                return configureOptionsFactory(factory);
            });
        }
    }
}
