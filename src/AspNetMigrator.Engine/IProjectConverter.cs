using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface IProjectConverter
    {
        Task<bool> ConvertAsync(string projectFilePath);
    }
}
