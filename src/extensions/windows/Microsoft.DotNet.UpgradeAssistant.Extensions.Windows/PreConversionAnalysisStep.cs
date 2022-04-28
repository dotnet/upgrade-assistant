// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;


namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    internal class PreConversionAnalysisStep : UpgradeStep
    {
        private ILogger<PreConversionAnalysisStep> _logger;

        private IPackageRestorer _packageRestorer;

        public PreConversionAnalysisStep(ILogger<PreConversionAnalysisStep> logger, IEnumerable<IPackageRestorer> restorer)
            : base(logger)
        {
            this._logger = logger;
            this._packageRestorer = restorer.First();
            //this._packageRestorer = new MSBuildPackageRestorer ;
        }

        public override string Title => "Perform pre conversion analysis";

        public override string Description => "Get all type information of the pre converted project";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // The user should select a specific project before backing up (since changes are only made at the project-level)
            WellKnownStepIds.CurrentProjectSelectionStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.BackupStepId
        };

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            await Task.Yield();
            await this._packageRestorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token);

            var project = context.CurrentProject!.GetRoslynProject();
            await context.ReloadWorkspaceAsync(token);
            var compilation = await project.GetCompilationAsync();

            compilation = compilation.AddReferences(new MetadataReference[] { 
                MetadataReference.CreateFromFile("C:\\Users\\ujchadha.NORTHAMERICA\\.nuget\\packages\\microsoft.windowsappsdk\\1.0.0\\lib\\uap10.0\\Microsoft.UI.Xaml.winmd"),
                MetadataReference.CreateFromFile("C:\\Users\\ujchadha.NORTHAMERICA\\.nuget\\packages\\microsoft.netcore.app.ref\\3.1.0\\ref\\netcoreapp3.1\\System.Collections.dll")});

            return new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, Id);
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            await Task.Yield();
            return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, Id, BuildBreakRisk.None);
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            await Task.Yield();
            var x = 1;
            return false;
        }
    }
}
