using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface ICollectUserInput
    {
        public Task<string> AskUser(string currentPath);
    }
}
