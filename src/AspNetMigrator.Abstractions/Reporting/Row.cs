using System.Collections.Generic;

namespace AspNetMigrator.Reporting
{
    public record Row(IReadOnlyCollection<object> Data)
    {
    }
}
