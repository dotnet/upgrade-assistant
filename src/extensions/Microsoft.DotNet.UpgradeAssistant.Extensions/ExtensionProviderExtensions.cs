using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public static class ExtensionProviderExtensions
    {
        private const string ConfigurationExtensionModulesSectionName = "ExtensionModules";
        private const string UpgradeAssistantExtensionPathsSettingName = "UpgradeAssistantExtensionPaths";

        /// <summary>
        /// Register extension services, including the default extension, aggregate extension, and any
        /// extensions found in specified paths.
        /// </summary>
        /// <param name="containerBuilder">The Autofac container builder to register services to.</param>
        /// <param name="configuration">The app configuraiton containing a setting for extension paths and the default extension modules. These extensions will be registered before those found with the string[] argument.</param>
        /// <param name="additionalExtensionPaths">Paths to probe for additional extensions. Can be paths to ExtensionManifest.json files, directories with such files, or zip files. These extensions will be registered after those found from configuration.</param>
        public static void RegisterExtensions(this ContainerBuilder containerBuilder, IConfiguration configuration, IEnumerable<string> additionalExtensionPaths)
        {
            if (containerBuilder is null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var extensionPathString = configuration[UpgradeAssistantExtensionPathsSettingName];
            var pathsFromString = extensionPathString?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();
            var extensionPaths = pathsFromString.Concat(additionalExtensionPaths);

            // Always include the default extension which contains built-in source updaters, config updaters, etc.
            containerBuilder.RegisterType<DefaultExtensionProvider>()
                .As<IExtensionProvider>()
                .SingleInstance();
            RegisterExtensionModules(containerBuilder, configuration);

            foreach (var e in extensionPaths)
            {
                if (string.IsNullOrEmpty(e))
                {
                    continue;
                }

                if (Directory.Exists(e))
                {
                    containerBuilder.Register(c => new DirectoryExtensionProvider(e, c.Resolve<ILogger<DirectoryExtensionProvider>>()));
                    RegisterExtensionModules(containerBuilder, DirectoryExtensionProvider.GetConfiguration(e));
                }
                else if (File.Exists(e))
                {
                    if (DirectoryExtensionProvider.ManifestFileName.Equals(Path.GetFileName(e), StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(e) ?? string.Empty;
                        containerBuilder.Register(c => new DirectoryExtensionProvider(dir, c.Resolve<ILogger<DirectoryExtensionProvider>>()));
                        RegisterExtensionModules(containerBuilder, DirectoryExtensionProvider.GetConfiguration(dir));
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

            containerBuilder.RegisterType<AggregateExtensionProvider>().SingleInstance();
        }

        private static void RegisterExtensionModules(ContainerBuilder containerBuilder, IConfiguration configuration)
        {
            var extensionModulePaths = configuration.GetSection(ConfigurationExtensionModulesSectionName)?.Get<string[]>();
            if (extensionModulePaths is not null)
            {
                foreach (var path in extensionModulePaths)
                {
                    try
                    {
                        containerBuilder.RegisterAssemblyModules(Assembly.LoadFrom(path));
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
