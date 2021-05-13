// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class FinalizeSolutionStep : UpgradeStep
    {
        private readonly IUserInput _userInput;
        private readonly UpgradeOptions _upgradeOptions;

        public FinalizeSolutionStep(IUserInput userInput, UpgradeOptions upgradeOptions, ILogger<FinalizeSolutionStep> logger)
            : base(logger)
        {
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _upgradeOptions = upgradeOptions ?? throw new ArgumentNullException(nameof(upgradeOptions));
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public override string Title => "Finalize upgrade";

        public override string Description => "All projects have been upgraded. Please review any changes and test accordingly.";

        public override string Id => WellKnownStepIds.FinalizeSolutionStepId;

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (_userInput.IsInteractive && !_upgradeOptions.PersistState.HasValue)
            {
                const string FileStateQuestion =
                    "For subsequent upgrades state is persisted in an .upgrade-assistant file, are you okay with that?";

                var choices = UpgradeCommand.CreateFromEnum<CommandChoice>();
                var result = await _userInput.ChooseAsync(FileStateQuestion, choices, token).ConfigureAwait(false);

                if (result.Value is CommandChoice.Yes)
                {
                    _upgradeOptions.PersistState = true;
                    context.GlobalProperties["PersistState"] = bool.TrueString;
                }
            }

            context.IsComplete = true;
            context.EntryPoints = Enumerable.Empty<IProject>();

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Upgrade complete");
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (!context.EntryPoints.Any())
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Upgrade completed", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Finalize upgrade", BuildBreakRisk.None));
            }
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(context.CurrentProject is null && context.EntryPoints.Any());
    }
}
