﻿// Licensed to the .NET Foundation under one or more agreements.
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
    {
        private readonly ExtensionServiceConfiguration _services;

        public ExtensionOptionsBuilder(ExtensionServiceConfiguration services)
        {
            _services = services;
        }

        public void MapFiles<TTo>(Func<TOption, string?> factory, bool isArray)
            => MapFiles<TTo>(o =>
            {
                var result = factory(o);

                return result is null ? Enumerable.Empty<string>() : new[] { result };
            }, isArray);

        private delegate ExtensionMappedFileConfigureOptions<TOption, TTo> ExtensionMappedConfigurationFactory<TTo>(Func<TOption, IEnumerable<string>> factory, bool isArray);

        public void MapFiles<TTo>(Func<TOption, IEnumerable<string>> factory, bool isArray)
        {
            _services.Services.TryAddTransient(typeof(ExtensionMappedFileConfigureOptions<,>));

            _services.Services.AddTransient<IConfigureOptions<OptionCollection<TTo>>>(ctx =>
            {
                var configureOptionsFactory = ctx.GetRequiredService<ExtensionMappedConfigurationFactory<TTo>>();

                return configureOptionsFactory(factory, isArray);
            });
        }
    }
}
