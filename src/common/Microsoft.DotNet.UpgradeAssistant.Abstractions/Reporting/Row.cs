using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Reporting
{
    public record Row(IReadOnlyCollection<object> Data)
    {
    }
}
