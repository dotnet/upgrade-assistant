// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class TestUpgradeStep : UpgradeStep
    {
        public int ApplicationCount { get; set; }

        public override string Id => typeof(TestUpgradeStep).FullName!;

        public override string Description { get; }

        public override string Title { get; }

        public TestUpgradeStep(
            string title,
            string? description = null,
            UpgradeStep? parentStep = null,
            IEnumerable<UpgradeStep>? subSteps = null,
            ILogger? logger = null)
            : base(logger ?? new NullLogger<TestUpgradeStep>())
        {
            Title = title;
            Description = description ?? string.Empty;
            ParentStep = parentStep;
            SubSteps = subSteps ?? Enumerable.Empty<UpgradeStep>();
            ApplicationCount = 0;
        }

        public virtual string AppliedMessage => "Test upgrade step complete";

        public virtual string InitializedMessage => "Test upgrade step incomplete";

        protected override bool IsApplicableImpl(IUpgradeContext context) => true;

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            ApplicationCount++;
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, AppliedMessage));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token) =>
            Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, InitializedMessage, BuildBreakRisk.Low));
    }
}
