using System.Collections.Generic;

namespace AspNetMigrator.PackageUpdater
{
    public record PackageUpdaterOptions(IEnumerable<string> PackageMapPaths);
}
