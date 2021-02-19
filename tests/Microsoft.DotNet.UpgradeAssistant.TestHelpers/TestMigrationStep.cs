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
    public class TestMigrationStep : MigrationStep
    {
        public int ApplicationCount { get; set; }

        public override string Id => typeof(TestMigrationStep).FullName!;

        public override string Description { get; }

        public override string Title { get; }

        public TestMigrationStep(
            string title,
            string? description = null,
            MigrationStep? parentStep = null,
            IEnumerable<MigrationStep>? subSteps = null,
            ILogger? logger = null)
            : base(logger ?? new NullLogger<TestMigrationStep>())
        {
            Title = title;
            Description = description ?? string.Empty;
            ParentStep = parentStep;
            SubSteps = subSteps ?? Enumerable.Empty<MigrationStep>();
            ApplicationCount = 0;
        }

        public virtual string AppliedMessage => "Test migration step complete";

        public virtual string InitializedMessage => "Test migration step incomplete";

        protected override bool IsApplicableImpl(IMigrationContext context) => true;

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            ApplicationCount++;
            return Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Complete, AppliedMessage));
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, InitializedMessage, BuildBreakRisk.Low));
    }
}
