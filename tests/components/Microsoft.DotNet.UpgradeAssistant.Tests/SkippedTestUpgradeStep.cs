// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class SkippedTestUpgradeStep : TestUpgradeStep
    {
        public override string Id => typeof(SkippedTestUpgradeStep).FullName!;

        private const string SkippedMessage = "Test upgrade step skipped";

        public override string AppliedMessage => SkippedMessage;

        public override string InitializedMessage => SkippedMessage;

        public SkippedTestUpgradeStep(string title, string? description = null, UpgradeStep? parentStep = null, IEnumerable<UpgradeStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token) =>
            Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, InitializedMessage, BuildBreakRisk.None));

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token) =>
            Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, AppliedMessage));
    }
}
