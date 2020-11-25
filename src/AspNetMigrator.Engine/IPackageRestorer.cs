using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface IPackageRestorer
    {
        /// <summary>
        /// Restores NuGet packages for a project and returns the location of
        /// the resulting lock file and package cache.
        /// </summary>
        /// <param name="context">The migration context to restore NuGet packages for.</param>
        /// <returns>A RestoreOutput object with the path to the project's lock file
        /// after restoring packages and the location of the NuGet package cache used during restore.</returns>
        Task<RestoreOutput> RestorePackagesAsync(IMigrationContext context, CancellationToken token);
    }
}
