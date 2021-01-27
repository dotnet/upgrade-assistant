using System;
using System.Collections.Generic;
using System.IO;
using AspNetMigrator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator
{
    public static class ExtensionProviderExtensions
    {
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
