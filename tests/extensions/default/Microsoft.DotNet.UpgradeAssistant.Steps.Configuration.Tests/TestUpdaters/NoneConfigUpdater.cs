// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Tests
{
    [ApplicableComponents(ProjectComponents.None)]
    public class NoneConfigUpdater : IUpdater<ConfigFile>
    {
        private readonly bool _isApplicable;

        public string Id => "Test ConfigUpdater";

        public string Title => "Test title";

        public string Description => "Test description";

        public BuildBreakRisk Risk { get; }

        public int ApplyCount { get; set; }

        public NoneConfigUpdater(BuildBreakRisk risk, bool isApplicable)
        {
            Risk = risk;
            _isApplicable = isApplicable;
        }

        public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            ApplyCount++;
            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(_isApplicable));
        }

        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(_isApplicable));
        }
    }
}
