using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AspNetMigrator.SourceUpdater
{
    /// <summary>
    /// Autofac module for registering Roslyn analyzers and code fix providers from a given directory.
    /// </summary>
    public class AnalyzersAndCodeFixersModule : Autofac.Module
    {
        private const string AssemblySearchPattern = "*.dll";

        public string SearchPath { get; set; }

        public AnalyzersAndCodeFixersModule(string searchPath)
        {
            SearchPath = searchPath ?? throw new ArgumentNullException(nameof(searchPath));
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Get the absolute search dir if the provided string is a relative path
            var searchDir = new DirectoryInfo(Path.IsPathRooted(SearchPath) ? SearchPath : Path.Combine(AppContext.BaseDirectory, SearchPath));

            if (!searchDir.Exists)
            {
                throw new InvalidOperationException($"Source updaters path ({searchDir.FullName}) not found");
            }

            // Attempt to load and register analyzers and code fix providers from each dll in the search path
            foreach (var file in searchDir.GetFiles(AssemblySearchPattern, SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file.FullName);

                    // Register analyzers
                    builder.RegisterAssemblyTypes(assembly)
                        .PublicOnly()
                        .Where(t => t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(DiagnosticAnalyzerAttribute))))
                        .As<DiagnosticAnalyzer>()
                        .InstancePerDependency();

                    // Register code fix providers
                    builder.RegisterAssemblyTypes(assembly)
                        .PublicOnly()
                        .Where(t => t.CustomAttributes.Any(a => a.AttributeType.Equals(typeof(ExportCodeFixProviderAttribute))))
                        .As<CodeFixProvider>()
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
