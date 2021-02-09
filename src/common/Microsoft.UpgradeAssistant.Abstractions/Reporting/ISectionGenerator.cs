using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant.Reporting
{
    public interface ISectionGenerator
    {
        Task<Section> GenerateContentAsync(IProject project, CancellationToken token);
    }
}
