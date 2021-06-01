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

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public static class ExtensionProviderExtensions
    {
        private const string ExtensionServiceProvidersSectionName = "ExtensionServiceProviders";
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

            services.AddSerializer();

            foreach (var extension in GetExtensions(configuration, additionalExtensionPaths))
            {
                services.AddExtension(extension);
            }
        }

        private static void AddSerializer(this IServiceCollection services)
        {
            services.AddOptions<JsonSerializerOptions>()
                .Configure(o =>
                {
                    o.AllowTrailingCommas = true;
                    o.ReadCommentHandling = JsonCommentHandling.Skip;
                    o.Converters.Add(new JsonStringEnumConverter());
                });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Will be disposed by dependency injection.")]
        private static IEnumerable<ExtensionInstance> GetExtensions(IConfiguration originalConfiguration, IEnumerable<string> additionalExtensionPaths)
        {
            foreach (var e in GetExtensionPaths())
            {
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }

                if (ExtensionInstance.Create(e) is ExtensionInstance instance)
                {
                    yield return instance;
                }
            }

            IEnumerable<string> GetExtensionPaths()
            {
                const string ExtensionDirectory = "extensions";

                var fromConfig = originalConfiguration.GetSection("DefaultExtensions")
                    .Get<string[]>()
                    .Select(n => Path.GetFullPath(Path.Combine(ExtensionDirectory, n)));

                var extensionPathString = originalConfiguration[UpgradeAssistantExtensionPathsSettingName];
                var pathsFromString = extensionPathString?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();

                return fromConfig.Concat(pathsFromString).Concat(additionalExtensionPaths);
            }
        }

        private static void AddExtension(this IServiceCollection services, ExtensionInstance extension)
        {
            services.AddSingleton(extension);

            var extensionServiceProviderPaths = extension.GetOptions<string[]>(ExtensionServiceProvidersSectionName);

            if (extensionServiceProviderPaths is null)
            {
                return;
            }

            foreach (var path in extensionServiceProviderPaths)
            {
                try
                {
                    var fileInfo = extension.FileProvider.GetFileInfo(path);

                    if (!fileInfo.Exists)
                    {
                        Console.WriteLine($"ERROR: Could not find extension service provider assembly {path} in extension {extension.Name}");
                        continue;
                    }

                    using var assemblyStream = fileInfo.CreateReadStream();

                    if (assemblyStream is null)
                    {
                        Console.WriteLine($"ERROR: Could not find extension service provider assembly {path} in extension {extension.Name}");
                        continue;
                    }

                    extension.LoadContext.LoadFromStream(assemblyStream);
                }
                catch (FileLoadException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }

            if (!extension.HasAssemblyLoadContext)
            {
                return;
            }

            var serviceProviders = extension.LoadContext.Assemblies.SelectMany(assembly => assembly
                .GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && typeof(IExtensionServiceProvider).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t))
                .Cast<IExtensionServiceProvider>());

            foreach (var sp in serviceProviders)
            {
                sp.AddServices(new ExtensionServiceCollection(services, extension));
            }
        }
    }
}
