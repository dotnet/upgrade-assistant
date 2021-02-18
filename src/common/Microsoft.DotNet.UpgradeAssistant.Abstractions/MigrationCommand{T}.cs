using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public abstract class MigrationCommand<T> : MigrationCommand
        where T : notnull
    {
        public T Value { get; init; } = default!;
    }
}
