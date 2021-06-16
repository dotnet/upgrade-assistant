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
        /// <param name="configuration">The app configuration containing a setting for extension paths and the default extension service providers. These extensions will be registered before those found with the string[] argument.</param>
        /// <param name="additionalExtensionPaths">Paths to probe for additional extensions. Can be paths to ExtensionManifest.json files, directories with such files, or zip files. These extensions will be registered after those found from configuration.</param>
        public static void AddExtensions(this IServiceCollection services, IConfiguration configuration, IEnumerable<string> additionalExtensionPaths)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions<ExtensionOptions>()
                .AddDefaultExtensions(configuration)
                .AddFromEnvironmentVariables(configuration)
                .Configure(options =>
                {
                    foreach (var path in additionalExtensionPaths)
                    {
                        options.ExtensionPaths.Add(path);
                    }
                });

            services.AddOptions<JsonSerializerOptions>()
                .Configure(o =>
                {
                    o.AllowTrailingCommas = true;
                    o.ReadCommentHandling = JsonCommentHandling.Skip;
                    o.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddScoped<ExtensionManager>();
            services.AddTransient<IEnumerable<ExtensionInstance>>(ctx => ctx.GetRequiredService<ExtensionManager>());
            services.AddExtensionLoaders();
        }

        private static void AddExtensionLoaders(this IServiceCollection services)
        {
            services.AddTransient<IExtensionLoader, DirectoryExtensionLoader>();
            services.AddTransient<IExtensionLoader, ManifestDirectoryExtensionLoader>();
            services.AddTransient<IExtensionLoader, ZipExtensionLoader>();
        }

        private static OptionsBuilder<ExtensionOptions> AddDefaultExtensions(this OptionsBuilder<ExtensionOptions> builder, IConfiguration configuration)
            => builder.Configure(options =>
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

        private static OptionsBuilder<ExtensionOptions> AddFromEnvironmentVariables(this OptionsBuilder<ExtensionOptions> builder, IConfiguration configuration)
            => builder.Configure(options =>
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
