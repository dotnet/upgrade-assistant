using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface ITargetFrameworkIdentifier
    {
        bool IsCoreCompatible(Stream projectFile);
    }
}
