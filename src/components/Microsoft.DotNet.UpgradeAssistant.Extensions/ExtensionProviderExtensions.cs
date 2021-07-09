// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        /// <param name="currentVersion">The current app version.</param>
        /// <param name="configuration">The app configuration containing a setting for extension paths and the default extension service providers. These extensions will be registered before those found with the string[] argument.</param>
        /// <param name="additionalExtensionPaths">Paths to probe for additional extensions. Can be paths to ExtensionManifest.json files, directories with such files, or zip files. These extensions will be registered after those found from configuration.</param>
        /// <returns>A builder for options</returns>
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

            services.AddSingleton<ExtensionManager>();
            services.AddTransient<IEnumerable<ExtensionInstance>>(ctx => ctx.GetRequiredService<ExtensionManager>());
            services.AddExtensionLoaders();

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
                const string DefaultExtensionsSection = "DefaultExtensions";

                var defaultExtensions = configuration.GetSection(DefaultExtensionsSection)
                    .Get<string[]>()
                    .Select(n => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ExtensionDirectory, n)));

                foreach (var path in defaultExtensions)
                {
                    options.ExtensionPaths.Add(path);
                }
            });
        }

        public static IServiceCollection AddExtensionOption<TOption>(this IServiceCollection services, TOption option)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<ExtensionOptions>()
                .Configure(options =>
                {
                    var json = JsonSerializer.SerializeToUtf8Bytes(option);
                    using var stream = new MemoryStream(json);

                    var config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();

                    options.Extensions.Add(new ExtensionInstance(new PhysicalFileProvider(Environment.CurrentDirectory), Environment.CurrentDirectory, config));
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
