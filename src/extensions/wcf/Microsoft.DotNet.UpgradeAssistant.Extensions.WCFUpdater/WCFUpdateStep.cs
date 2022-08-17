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
    [ApplicableLanguage(Language.CSharp)]
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
            _path = new Dictionary<string, IEnumerable<string>>();
            FindPath(project);
            if (_path.Count != 3 && _path.Count != 4)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Cannot find required file(s) for WCF update. The project is not applicable for automated update and this step is complete.", BuildBreakRisk.Low));
            }

            // if the project is not applicable, skip the update step
            if (!WCFUpdateChecker.IsWCFUpdateApplicable(_path, Logger))
            {
                var message = "This project is not applicable for updating to CoreWCF since references to WCF were not found. No more work needs to be done and this step is complete.";
                Logger.LogInformation(message);
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, message, BuildBreakRisk.Low));
            }

            // construct updaters
            _configUpdater = UpdaterFactory.GetConfigUpdater(_path["config"].First(), Logger);
            _packageUpdater = UpdaterFactory.GetPackageUpdater(_path["proj"].First(), Logger);
            if (_path.ContainsKey("directives"))
            {
                _directiveUpdaters = UpdaterFactory.GetDirectiveUpdaters(_path["directives"], Logger);
            }

            if (_configUpdater is not null)
            {
                var configContext = UpdateRunner.GetContext(_configUpdater);
                _sourceCodeUpdater = UpdaterFactory.GetSourceCodeUpdater(_path["main"].First(), configContext, Logger);
            }

            if (_configUpdater is null || _packageUpdater is null || _sourceCodeUpdater is null || (_path.ContainsKey("directives") && _directiveUpdaters is null))
            {
                Logger.LogWarning("Unexpected error happened when trying to construct the updaters. Please review error message and log.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Updaters cannot be constructed. Please review error message and log for more information.", BuildBreakRisk.Medium));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "WCF updaters are initialized. Ready to update.", BuildBreakRisk.None));
        }

        public Task<UpgradeStepApplyResult> Apply()
        {
            // Run updates
            var projFile = UpdateRunner.PackageUpdate(_packageUpdater!, Logger);
            var config = UpdateRunner.ConfigUpdate(_configUpdater!, Logger);
            var source = UpdateRunner.SourceCodeUpdate(_sourceCodeUpdater!, Logger);
            var directives = new List<SyntaxNode>();
            if (_directiveUpdaters is not null)
            {
                directives = UpdateRunner.DirectiveUpdate(_directiveUpdaters, Logger);
            }

            // check for null changes
            if (projFile is null || config is null || source is null || directives is null)
            {
                Logger.LogWarning("Unexpected error happened when trying to making changes to original files. Please review error message and log.");
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log for more information."));
            }

            // Write updates
            UpdateRunner.WritePackageUpdate(projFile, _path!["proj"].First(), Logger);
            UpdateRunner.WriteConfigUpdate(config[0], config[1], _path["config"].First(), Logger);
            UpdateRunner.WriteSourceCodeUpdate(source, _path["main"].First(), Logger);
            if (directives.Count > 0)
            {
                UpdateRunner.WriteDirectiveUpdate(directives, _path["directives"], Logger);
            }

            Logger.LogInformation("Project was successfully updated to use CoreWCF services. Please review changes.");
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project was successfully ported to CoreWCF."));
        }

        private void FindPath(IProject project)
        {
            var csFile = project.FindFiles(".cs", ProjectItemType.Compile);
            var main = from f in csFile
                       where File.Exists(f) && File.ReadAllText(f).Replace(" ", string.Empty).Contains("Main(", StringComparison.Ordinal)
                       select f;
            var directives = from f in csFile
                             where File.Exists(f) && File.ReadAllText(f).Contains("using System.ServiceModel", StringComparison.Ordinal) && !File.ReadAllText(f).Replace(" ", string.Empty).Contains("Main(", StringComparison.Ordinal)
                             select f;
            var config = from f in project.FindFiles(".config", ProjectItemType.None)
                         where File.Exists(f) && File.ReadAllText(f).Contains("<system.serviceModel>", StringComparison.Ordinal)
                         select f;

            if (!main.Any())
            {
                Logger.LogWarning("Can not find .cs file with Main() method. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else if (main.Count() > 1)
            {
                Logger.LogWarning("Found more than one .cs file with Main() method. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else if (!config.Any())
            {
                if (File.ReadAllText(main.First()).Contains("ServiceHost"))
                {
                    Logger.LogWarning("ServiceHost instance was detected in code but can not find .config file that configures system.serviceModel. " +
                        "Automated update cannot be applied. Please update the project to CoreWCF manually (https://github.com/CoreWCF/CoreWCF).");
                }

                Logger.LogWarning("Did not find .config file that configures system.serviceModel. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else
            {
                _path!.Add("main", main);
                Logger.LogTrace($"This following file: {main.First()} needs source code update to replace ServiceHost instance.");
                _path.Add("config", config);
                Logger.LogTrace($"This following config file: {config.First()} needs to be updated.");
                _path.Add("proj", new[] { project.GetFile().FilePath });
                Logger.LogTrace($"This following project file: {project.GetFile().FilePath} needs to be updated");

                if (directives.Any())
                {
                    Logger.LogDebug($"This .cs file: {directives.First()} needs using directives updates. Adding the path to collection.");
                    _path.Add("directives", directives);
                }

                Logger.LogDebug("Retrieved file paths that are needed for updates.");
            }
        }
    }
}
