// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
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
            var defaultExtension = new DefaultExtension(configuration);
            services.AddSingleton<IExtension>(defaultExtension);
            RegisterExtensionServices(services, defaultExtension, configuration);

            foreach (var e in extensionPaths)
            {
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }

                if (Directory.Exists(e))
                {
                    var extension = new DirectoryExtension(e);
                    services.AddSingleton<IExtension>(extension);
                    RegisterExtensionServices(services, extension, DirectoryExtension.GetConfiguration(e));
                }
                else if (File.Exists(e))
                {
                    if (DirectoryExtension.ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(e) ?? string.Empty;
                        var extension = new DirectoryExtension(dir);
                        services.AddSingleton<IExtension>(extension);
                        RegisterExtensionServices(services, extension, DirectoryExtension.GetConfiguration(dir));
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

        private static void RegisterExtensionServices(IServiceCollection services, IExtension extension, IConfiguration extensionConfiguration)
        {
            var extensionServiceProviderPaths = extension.GetOptions<string[]>(ExtensionServiceProvidersSectionName);
            if (extensionServiceProviderPaths is null)
            {
                return;
            }

            foreach (var path in extensionServiceProviderPaths)
            {
                try
                {
                    using var assemblyStream = extension.GetFile(path);
                    if (assemblyStream is null)
                    {
                        Console.WriteLine($"ERROR: Could not find extension service provider assembly {path} in extension {extension.Name}");
                        continue;
                    }

                    var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
                    var serviceProviders = assembly.GetTypes()
                        .Where(t => t.IsPublic && !t.IsAbstract && t.IsAssignableTo(typeof(IExtensionServiceProvider)))
                        .Select(t => Activator.CreateInstance(t))
                        .Cast<IExtensionServiceProvider>();

                    foreach (var sp in serviceProviders)
                    {
                        sp.AddServices(new ExtensionServiceConfiguration(services, extensionConfiguration));
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
