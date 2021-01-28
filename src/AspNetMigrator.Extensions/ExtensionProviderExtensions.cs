using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AspNetMigrator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class ExtensionProviderExtensions
    {
        /// <summary>
        /// Register extension services, including the default extension, aggregate extension, and any
        /// extensions found in specified paths.
        /// </summary>
        /// <param name="services">The service collection to register services to.</param>
        /// <param name="extensionPathString">A ;-delimited list of paths to probe for extensions. These extensions will be registered before those found with the string[] argument.</param>
        /// <param name="extensionPaths">Paths to probe for additional extensions. Can be paths to ExtensionManifest.json files, directories with such files, or zip files. These extensions will be registered after those found with the string argument.</param>
        /// <returns>The service collection with extension services registered.</returns>
        public static IServiceCollection AddExtensions(this IServiceCollection services, string? extensionPathString, IEnumerable<string> extensionPaths)
        {
            var pathsFromString = extensionPathString?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
            return AddExtensions(services, pathsFromString.Concat(extensionPaths));
        }

        /// <summary>
        /// Register extension services, including the default extension, aggregate extension, and any
        /// extensions found in specified paths.
        /// </summary>
        /// <param name="services">The service collection to register services to.</param>
        /// <param name="extensionPaths">Paths to probe for additional extensions. Can be paths to ExtensionManifest.json files, directories with such files, or zip files.</param>
        /// <returns>The service collection with extension services registered.</returns>
        public static IServiceCollection AddExtensions(this IServiceCollection services, IEnumerable<string> extensionPaths)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (extensionPaths is null)
            {
                throw new ArgumentNullException(nameof(extensionPaths));
            }

            // Always include the default extension which contains built-in source updaters, config updaters, etc.
            services.AddSingleton<IExtensionProvider, DefaultExtensionProvider>();

            foreach (var e in extensionPaths)
            {
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }

                if (Directory.Exists(e))
                {
                    services.AddSingleton<IExtensionProvider>(sp => ActivatorUtilities.CreateInstance<DirectoryExtensionProvider>(sp, e));
                }
                else if (File.Exists(e))
                {
                    if (DirectoryExtensionProvider.ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        services.AddSingleton<IExtensionProvider>(sp => ActivatorUtilities.CreateInstance<DirectoryExtensionProvider>(sp, Path.GetDirectoryName(e) ?? string.Empty));
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

            services.AddSingleton<AggregateExtensionProvider>();

            return services;
        }
    }
}
