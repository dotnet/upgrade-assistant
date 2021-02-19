// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class SkippedTestMigrationStep : TestMigrationStep
    {
        public override string Id => typeof(SkippedTestMigrationStep).FullName!;

        private const string SkippedMessage = "Test migration step skipped";

        public override string AppliedMessage => SkippedMessage;

        public override string InitializedMessage => SkippedMessage;

        public SkippedTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Skipped, InitializedMessage, BuildBreakRisk.None));

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Skipped, AppliedMessage));
    }
}
