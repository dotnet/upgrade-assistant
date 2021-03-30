// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Tests
{
    public class FailingConfigUpdater : IUpdater<ConfigFile>
    {
        public string Id => "Test ConfigUpdater";

        public string Title => "Test title";

        public string Description => "Test description";

        public BuildBreakRisk Risk { get; }

        public int ApplyCount { get; set; }

        public FailingConfigUpdater(BuildBreakRisk risk)
        {
            Risk = risk;
        }

        public Task<bool> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token) =>
            throw new NotImplementedException();

        public Task<bool> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token) =>
            throw new NotImplementedException();
    }
}
