// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// <param name="configuration">The app configuraiton containing a setting for extension paths and the default extension service providers. These extensions will be registered before those found with the string[] argument.</param>
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

            var extensionPathString = configuration[UpgradeAssistantExtensionPathsSettingName];
            var pathsFromString = extensionPathString?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
            var extensionPaths = pathsFromString.Concat(additionalExtensionPaths);

            // Always include the default extension which contains built-in source updaters, config updaters, etc.
            services.AddSingleton<IExtension, DefaultExtension>();
            RegisterExtensionServices(services, configuration);

            foreach (var e in extensionPaths)
            {
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }

                if (Directory.Exists(e))
                {
                    services.AddSingleton<IExtension>(sp => ActivatorUtilities.CreateInstance<DirectoryExtension>(sp, e));
                    RegisterExtensionServices(services, DirectoryExtension.GetConfiguration(e));
                }
                else if (File.Exists(e))
                {
                    if (DirectoryExtension.ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(e) ?? string.Empty;
                        services.AddSingleton<IExtension>(sp => ActivatorUtilities.CreateInstance<DirectoryExtension>(sp, dir));
                        RegisterExtensionServices(services, DirectoryExtension.GetConfiguration(dir));
                    }
                    else if (Path.GetExtension(e).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"ERROR: Archive extensions not yet supported; ignoring extension {e}");
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Extension {e} not found; ignoring extension {e}");
                }
            }

            services.AddSingleton<AggregateExtension>();
        }

        private static void RegisterExtensionServices(IServiceCollection services, IConfiguration configuration)
        {
            var extensionServiceProviderPaths = configuration.GetSection(ExtensionServiceProvidersSectionName)?.Get<string[]>();
            if (extensionServiceProviderPaths is not null)
            {
                foreach (var path in extensionServiceProviderPaths)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(path);
                        var serviceProviders = assembly.GetTypes()
                            .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IExtensionServiceProvider)))
                            .Select(t => Activator.CreateInstance(t))
                            .Cast<IExtensionServiceProvider>();

                        foreach (var sp in serviceProviders)
                        {
                            sp.AddServices(services, configuration);
                        }
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }
            }
        }
    }
}
