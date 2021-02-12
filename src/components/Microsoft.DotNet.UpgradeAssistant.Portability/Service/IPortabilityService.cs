using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Service
{
    public interface IPortabilityService
    {
        IAsyncEnumerable<ApiInformation> GetApiInformation(IReadOnlyCollection<string> apis, CancellationToken token);
    }
}
