using System.IO;

namespace AspNetMigrator
{
    public interface ITargetFrameworkIdentifier
    {
        bool IsCoreCompatible(Stream projectFile);
    }
}
