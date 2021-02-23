// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UnknownTestUpgradeStep : TestUpgradeStep
    {
        public override string Id => typeof(UnknownTestUpgradeStep).FullName!;

        private const string UnknownMessage = "Test upgrade status unknown";

        public override string AppliedMessage => UnknownMessage;

        public override string InitializedMessage => UnknownMessage;

        public UnknownTestUpgradeStep(string title, string? description = null, UpgradeStep? parentStep = null, IEnumerable<UpgradeStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token) =>
            Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Unknown, InitializedMessage, BuildBreakRisk.Unknown));

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token) =>
            Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, AppliedMessage));
    }
}
