﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public static class ExtensionProviderExtensions
    {
        private const string UpgradeAssistantExtensionPathsSettingName = "UpgradeAssistantExtensionPaths";

        /// <summary>
        /// Register extension services, including the default extension, aggregate extension, and any
        /// extensions found in specified paths.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        /// <returns>A builder for options.</returns>
        public static OptionsBuilder<ExtensionOptions> AddExtensions(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<JsonSerializerOptions>()
                .Configure(o =>
                {
                    o.AllowTrailingCommas = true;
                    o.ReadCommentHandling = JsonCommentHandling.Skip;
                    o.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddSingleton<ExtensionInstanceFactory>();
            services.AddTransient<IExtensionManager, ExtensionManager>();
            services.AddSingleton<IExtensionProvider, ExtensionProvider>();
            services.AddExtensionLoaders();
            services.TryAddSingleton<IUpgradeAssistantConfigurationLoader, DefaultUpgradeAssistantConfigurationLoader>();
            services.TryAddTransient<IExtensionDownloader, NuGetExtensionDownloader>();
            services.AddTransient<IExtensionLocator, ExtensionLocator>();
            services.TryAddTransient<IExtensionCreator, NuGetExtensionPackageCreator>();

            return services.AddOptions<ExtensionOptions>();
        }

        public static IEnumerable<AdditionalOption> ParseOptions(this IEnumerable<string> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var option in options)
            {
                var split = option.Split('=');

                if (split.Length == 2)
                {
                    yield return new AdditionalOption(split[0], split[1]);
                }
            }
        }

        private static void AddExtensionLoaders(this IServiceCollection services)
        {
            services.AddTransient<IExtensionLoader, DirectoryExtensionLoader>();
            services.AddTransient<IExtensionLoader, ManifestDirectoryExtensionLoader>();
            services.AddTransient<IExtensionLoader, ZipExtensionLoader>();
        }

        public static OptionsBuilder<ExtensionOptions> AddDefaultExtensions(this OptionsBuilder<ExtensionOptions> builder, IConfiguration configuration)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Configure(options =>
            {
                const string ExtensionDirectory = "extensions";
                var settings = configuration.GetSection("Extensions").Get<ExtensionSettings>();

                options.DefaultSource = settings.Source;

                AddExtensions(options.DefaultExtensions, settings.Default);
                AddExtensions(options.RequiredExtensions, settings.Required);

                static void AddExtensions(ICollection<string> collection, string[] names)
                {
                    var extensionFullPaths = names
                        .Select(n => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ExtensionDirectory, n)));

                    foreach (var path in extensionFullPaths)
                    {
                        collection.Add(path);
                    }
                }
            });
        }

        private class ExtensionSettings
        {
            public string Source { get; set; } = string.Empty;

            public string[] Default { get; set; } = Array.Empty<string>();

            public string[] Required { get; set; } = Array.Empty<string>();
        }

        public static IServiceCollection AddExtensionOption<TOption>(this IServiceCollection services, TOption option)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<ExtensionOptions>()
                .Configure<ExtensionInstanceFactory>((options, factory) =>
                {
                    var json = JsonSerializer.SerializeToUtf8Bytes(option);
                    using var stream = new MemoryStream(json);

                    var config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();

                    options.Extensions.Add(factory.Create(new PhysicalFileProvider(Environment.CurrentDirectory), Environment.CurrentDirectory, config));
                });

            return services;
        }

        public static OptionsBuilder<ExtensionOptions> AddFromEnvironmentVariables(this OptionsBuilder<ExtensionOptions> builder, IConfiguration configuration)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Configure(options =>
            {
                var extensionPathString = configuration[UpgradeAssistantExtensionPathsSettingName];
                var pathsFromString = extensionPathString?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();

                foreach (var path in pathsFromString)
                {
                    options.ExtensionPaths.Add(path);
                }
            });
        }
    }
}
