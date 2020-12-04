using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater
{
    public interface IPackageLoader
    {
        Task<PackageArchiveReader?> GetPackageArchiveAsync(NuGetReference packageReference, CancellationToken token, string? cachePath = null);

        Task<IEnumerable<NuGetVersion>> GetNewerVersionsAsync(string packageName, NuGetVersion currentVersion, bool latestMinorAndBuildOnly, CancellationToken token);
    }
}
