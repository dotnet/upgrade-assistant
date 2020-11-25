using System.Collections.Generic;

namespace AspNetMigrator.Engine
{
    public record PackageUpdaterOptions(IEnumerable<string> PackageMapPaths);
}
