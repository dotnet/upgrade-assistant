using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class SetTFMStep : MigrationStep
    {
        private readonly IPackageRestorer _restorer;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be SDK-style before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.TryConvertProjectConverterStep",
        };

        public SetTFMStep(IPackageRestorer restorer, ILogger<SetTFMStep> logger)
            : base(logger)
        {
            _restorer = restorer;
        }

        public override string Id => typeof(SetTFMStep).FullName!;

        public override string Title => "Update TFM";

        public override string Description => "Update TFM for current project";

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            var projectInfo = context.CurrentProject.Required();

            var file = projectInfo.Project.GetFile();
            file.SetTFM(projectInfo.TargetTFM);
            await file.SaveAsync(token);

            // With an updated TFM, we should restore packages
            await _restorer.RestoreAllProjectPackagesAsync(context, token);

            return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Updated TFM to {projectInfo.TargetTFM}");
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            var projectInfo = context.CurrentProject.Required();

            if (projectInfo.TargetTFM == projectInfo.Project.TFM)
            {
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"TFM needs to be updated to {projectInfo.TargetTFM}", BuildBreakRisk.High));
            }
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context?.CurrentProject is not null;
    }
}
