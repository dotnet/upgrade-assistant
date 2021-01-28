using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.Portability
{
    /// <summary>
    /// An interface to define issues with portability. This may pull data from the Portability service, as well as config files, etc.
    /// </summary>
    internal interface IPortabilityAnalyzer
    {
        IAsyncEnumerable<PortabilityResult> Analyze(Compilation compilation, CancellationToken token);
    }
}
