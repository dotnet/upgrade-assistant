using System;
using System.IO;
using System.Reflection;
using Autofac;

namespace AspNetMigrator.ConfigUpdater
{
    public class ConfigUpdatersModule : Autofac.Module
    {
        private const string AssemblySearchPattern = "*.dll";

        public string SearchPath { get; set; }

        public ConfigUpdatersModule(string searchPath)
        {
            SearchPath = searchPath ?? throw new ArgumentNullException(nameof(searchPath));
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Get the absolute search dir if the provided string is a relative path
            var searchDir = new DirectoryInfo(Path.IsPathRooted(SearchPath) ? SearchPath : Path.Combine(AppContext.BaseDirectory, SearchPath));

            if (!searchDir.Exists)
            {
                throw new InvalidOperationException($"Config updaters path ({searchDir.FullName}) not found");
            }

            // Attempt to load and register analyzers and code fix providers from each dll in the search path
            foreach (var file in searchDir.GetFiles(AssemblySearchPattern, SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file.FullName);

                    builder.RegisterAssemblyTypes(assembly)
                        .PublicOnly()
                        .Where(t => t.IsAssignableTo<IConfigUpdater>())
                        .As<IConfigUpdater>()
                        .InstancePerDependency();
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
