// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class WCFUpdateStep : UpgradeStep
    {
        public override string Title => "Update WCF service to CoreWCF.";

        public override string Description => "Update WCF service to use CoreWCF services. For more information about CoreWCF, please go to: https://github.com/CoreWCF/CoreWCF";

        public override string Id => WellKnownStepIds.WCFUpdateStepId;

        private ConfigUpdater? _configUpdater;

        private SourceCodeUpdater? _sourceCodeUpdater;

        private PackageUpdater? _packageUpdater;

        private List<SourceCodeUpdater>? _directiveUpdaters;

        private Dictionary<string, IEnumerable<string>>? _path;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing source
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to changing source (since some code fixers will change added templates)
            WellKnownStepIds.TemplateInserterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,

            // Project's config file, source code, and project file need to be updated first before adding CoreWCF services
            WellKnownStepIds.ConfigUpdaterStepId,

            WellKnownStepIds.SourceUpdaterStepId,

            WellKnownStepIds.PackageUpdaterStepId
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public WCFUpdateStep(ILogger<WCFUpdateStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var currentProject = context.CurrentProject.Required();
                return Initialize(currentProject);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Unexpected error happened during CoreWCF porting.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log file for more information.", BuildBreakRisk.Medium));
            }
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                return Apply();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Unexpected error happened during CoreWCF porting.");
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log file for more information."));
            }
        }

        public Task<UpgradeStepInitializeResult> Initialize(IProject project)
        {
            FindPath(project);
            if (_path.Count == 0)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "Cannot find required file(s) for WCF update. The project is not applicable and will be skipped.", BuildBreakRisk.Low));
            }

            // if the project is not applicable, skip the update step
            if (!WCFUpdateChecker.IsWCFUpdateApplicable(_path, Logger))
            {
                Logger.LogInformation("This project is not applicable for updating to CoreWCF since references to WCF was not found.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "WCF Update step is not applicable to this project and will be skipped.", BuildBreakRisk.Low));
            }

            // construct updaters
            _configUpdater = UpdaterFactory.GetConfigUpdater(_path["config"].First(), Logger);
            _packageUpdater = UpdaterFactory.GetPackageUpdater(_path["proj"].First(), Logger);
            if (_path.ContainsKey("directives"))
            {
                _directiveUpdaters = UpdaterFactory.GetDirectiveUpdaters(_path["directives"], Logger);
            }

            if (_configUpdater != null)
            {
                var configContext = UpdateRunner.GetContext(_configUpdater);
                _sourceCodeUpdater = UpdaterFactory.GetSourceCodeUpdater(_path["main"].First(), configContext, Logger);
            }

            if (_configUpdater == null || _packageUpdater == null || _sourceCodeUpdater == null || (_path.ContainsKey("directives") && _directiveUpdaters == null))
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Updaters cannot be constructed. Please review error message and log file for more information.", BuildBreakRisk.Medium));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "WCF updaters are initialized. Ready to update.", BuildBreakRisk.None));
        }

        public Task<UpgradeStepApplyResult> Apply()
        {
            // Run updates
            var projFile = UpdateRunner.PackageUpdate(_packageUpdater, Logger);
            var config = UpdateRunner.ConfigUpdate(_configUpdater, Logger);
            var source = UpdateRunner.SourceCodeUpdate(_sourceCodeUpdater, Logger);
            var directives = new List<SyntaxNode>();
            if (_directiveUpdaters != null)
            {
                directives = UpdateRunner.DirectiveUpdate(_directiveUpdaters, Logger);
            }

            // check for null
            if (projFile == null || config == null || source == null || directives == null)
            {
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log file for more information."));
            }

            // Write updates
            UpdateRunner.WritePackageUpdate(projFile, _path["proj"].First(), Logger);
            UpdateRunner.WriteConfigUpdate(config[0], config[1], _path["config"].First(), Logger);
            UpdateRunner.WriteSourceCodeUpdate(source, _path["main"].First(), Logger);
            if (directives.Count > 0)
            {
                UpdateRunner.WriteDirectiveUpdate(directives, _path["directives"], Logger);
            }

            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project was successfully ported to CoreWCF. Please review changes."));
        }

        private void FindPath(IProject project)
        {
            _path = new Dictionary<string, IEnumerable<string>>();
            var csFile = project.FindFiles(".cs", ProjectItemType.Compile);
            var main = from f in csFile
                       where File.Exists(f) && File.ReadAllText(f).Contains("Main()")
                       select f;
            var directives = from f in csFile
                             where File.Exists(f) && File.ReadAllText(f).Contains("using System.ServiceModel") && !File.ReadAllText(f).Contains("Main()")
                             select f;
            var config = from f in project.FindFiles(".config", ProjectItemType.None)
                         where File.Exists(f) && File.ReadAllText(f).Contains("<system.serviceModel>")
                         select f;

            if (!main.Any())
            {
                Logger.LogWarning("Can not find .cs file with main method.");
            }
            else if (main.Count() > 1)
            {
                Logger.LogWarning("Find more than one .cs file with main method.");
            }
            else if (!config.Any())
            {
                Logger.LogWarning("Can not find .config file that configures system.serviceModel.");
            }
            else
            {
                _path.Add("main", main);
                _path.Add("config", config);
                _path.Add("proj", new[] { project.GetFile().FilePath });
                if (directives.Any())
                {
                    _path.Add("directives", directives);
                }
            }
        }
    }
}
