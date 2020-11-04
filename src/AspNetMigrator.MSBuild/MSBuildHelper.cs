using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Build.Locator;

namespace AspNetMigrator.MSBuild
{
    public static class MSBuildHelper
    {
        public static string RegisterMSBuildInstance()
        {
            // TODO : Harden this and allow MSBuild location to be read from env vars.
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().First();
            MSBuildLocator.RegisterInstance(msBuildInstance);
            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
            {
                // TODO : Harden this and extract the event handler to its own class
                if (context is null || assemblyName is null)
                {
                    return null;
                }

                // If the assembly has a culture, check for satellite assemblies
                if (assemblyName.CultureInfo != null)
                {
                    var satellitePath = Path.Combine(msBuildInstance.MSBuildPath, assemblyName.CultureInfo.Name, $"{assemblyName.Name}.dll");
                    if (File.Exists(satellitePath))
                    {
                        return context.LoadFromAssemblyPath(satellitePath);
                    }

                    satellitePath = Path.Combine(msBuildInstance.MSBuildPath, assemblyName.CultureInfo.TwoLetterISOLanguageName, $"{assemblyName.Name}.dll");
                    if (File.Exists(satellitePath))
                    {
                        return context.LoadFromAssemblyPath(satellitePath);
                    }
                }

                var assemblyPath = Path.Combine(msBuildInstance.MSBuildPath, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }

                // TODO : Log missing assembly
                return null;
            };

            return msBuildInstance.MSBuildPath;
        }
    }
}
