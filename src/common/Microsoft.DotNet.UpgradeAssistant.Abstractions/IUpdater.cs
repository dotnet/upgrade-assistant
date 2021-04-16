// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpdater<T>
    {
        string Id { get; }

        string Title { get; }

        string Description { get; }

        BuildBreakRisk Risk { get; }

        Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<T> inputs, CancellationToken token);

        Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<T> inputs, CancellationToken token);
    }
}
