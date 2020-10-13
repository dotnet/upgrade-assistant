using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface ISourceUpdater
    {
        Task<bool> UpdateSourceAsync(string projectFilePath);
    }
}
