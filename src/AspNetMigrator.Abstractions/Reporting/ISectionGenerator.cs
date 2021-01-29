using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Reporting
{
    public interface ISectionGenerator
    {
        Task<Section> GenerateContentAsync(IProject project, CancellationToken token);
    }
}
