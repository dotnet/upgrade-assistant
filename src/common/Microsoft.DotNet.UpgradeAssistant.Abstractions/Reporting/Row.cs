using System.Collections.Generic;

namespace Microsoft.UpgradeAssistant.Reporting
{
    public record Row(IReadOnlyCollection<object> Data)
    {
    }
}
