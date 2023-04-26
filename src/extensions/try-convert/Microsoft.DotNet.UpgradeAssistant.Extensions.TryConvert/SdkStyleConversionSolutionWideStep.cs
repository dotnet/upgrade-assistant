// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class SdkStyleConversionSolutionWideStep : UpgradeStep
    {
        private readonly TryConvertRunner _runner;

        private IEnumerable<UpgradeStep>? _substeps;

        public override string Description => $"Use the try-convert tool ({_runner.Path}{_runner.VersionString}) to convert the projects in a solution";

        public override string Title => $"Convert projects in a solution to SDK style";

        public override string Id => WellKnownStepIds.TryConvertProjectConverterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.EntrypointSelectionStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.CurrentProjectSelectionStepId,
        };

        public SdkStyleConversionSolutionWideStep(
            TryConvertRunner runner,
            ILogger<SdkStyleConversionSolutionWideStep> logger)
            : base(logger)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is null);

        public override IEnumerable<UpgradeStep> SubSteps => _substeps ?? Enumerable.Empty<UpgradeStep>();

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_substeps is null)
            {
                var all = context.EntryPoints.PostOrderTraversal(t => t.ProjectReferences)
                    .Select(p => (new ProjectSdkConversionStep(_runner, p.FileInfo, Logger), p.GetFile().IsSdk))
                    .ToList();

                _substeps = all.Select(a => a.Item1);

                if (all.All(a => a.IsSdk))
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "ALl projects are already SDK style.", BuildBreakRisk.None));
                }
                else
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Some projects need to be converted to SDK style.", BuildBreakRisk.High));
                }
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Projects have already been converted to SDK style projects.", BuildBreakRisk.None));
            }
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "All projects in solution are now updated to SDK style project."));

        private class ProjectSdkConversionStep : UpgradeStep
        {
            private readonly TryConvertRunner _runner;
            private readonly FileInfo _project;

            public ProjectSdkConversionStep(TryConvertRunner runner, FileInfo project, ILogger logger)
                : base(logger)
            {
                _project = project;
                _runner = runner;
            }

            public override string Title => $"Update {_project.Name} project format";

            public override string Description => $"Convert {_project.Name} from old-style project format to SDK style project.";

            protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
                => _runner.ApplyAsync(this, context, context.GetProject(_project.FullName), token);

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
                => _runner.InitializeAsync(context.GetProject(_project.FullName), token);

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
