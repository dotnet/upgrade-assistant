// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class TryConvertProjectConverterStep : UpgradeStep
    {
        internal const string StepTitle = $"Convert project file to SDK style";

        private readonly TryConvertRunner _runner;

        public override string Description => $"Use the try-convert tool ({_runner.Path}{_runner.VersionString}) to convert the project file to an SDK-style csproj";

        public override string Title => StepTitle;

        public override string Id => WellKnownStepIds.TryConvertProjectConverterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            WellKnownStepIds.BackupStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public TryConvertProjectConverterStep(
            TryConvertRunner runner,
            ILogger<TryConvertProjectConverterStep> logger)
            : base(logger)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _runner.ApplyAsync(context, context.CurrentProject.Required(), token);
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            return _runner.InitializeAsync(project, token);
        }
    }
}
