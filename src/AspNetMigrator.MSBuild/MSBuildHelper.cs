using System.Linq;
using Microsoft.Build.Locator;

namespace AspNetMigrator.MSBuild
{
    public static class MSBuildHelper
    {
        public static void RegisterMSBuildInstance()
        {
            // TODO : Make this more correct
            //var msBuildPath = Path.Combine(Environment.GetEnvironmentVariable("VSINSTALLDIR"), "MSBuild", "Current", "Bin");
            //MSBuildLocator.Re
            //LooseVersionAssemblyLoader.Register(msBuildPath);

            var instances = MSBuildLocator.QueryVisualStudioInstances();
            MSBuildLocator.RegisterInstance(instances.First());
        }
    }
}
