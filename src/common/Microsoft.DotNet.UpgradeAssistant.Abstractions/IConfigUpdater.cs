﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IConfigUpdater
    {
        string Id { get; }

        string Title { get; }

        string Description { get; }

        BuildBreakRisk Risk { get; }

        Task<bool> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token);

        Task<bool> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token);
    }
}
