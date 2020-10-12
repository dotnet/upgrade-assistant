using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface IPackageUpdater
    {
        Task<bool> UpdatePackagesAsync(string projectFilePath);
    }
}
