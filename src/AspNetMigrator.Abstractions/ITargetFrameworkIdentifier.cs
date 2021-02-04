using System.Collections.Generic;

namespace AspNetMigrator
{
    public interface ITargetFrameworkIdentifier
    {
        bool IsCoreCompatible(IEnumerable<TargetFrameworkMoniker> tfms);
    }
}
