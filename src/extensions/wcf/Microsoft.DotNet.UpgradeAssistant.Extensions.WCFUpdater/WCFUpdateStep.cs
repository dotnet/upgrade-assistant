// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    [ApplicableLanguage(Language.CSharp)]
    public class WCFUpdateStep : UpgradeStep
    {
        public override string Title => "Update WCF service to CoreWCF (Preview)";

        public override string Description => "Update WCF service to use CoreWCF services. For more information about CoreWCF, please go to: https://github.com/CoreWCF/CoreWCF";

        public override string Id => WellKnownStepIds.WCFUpdateStepId;

        private ConfigUpdater? _configUpdater;

        private SourceCodeUpdater? _sourceCodeUpdater;

        private PackageUpdater? _packageUpdater;

        private List<SourceCodeUpdater>? _directiveUpdaters;

        private FilePath _path;

        private ILoggerFactory _loggerFactory;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing source
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to changing source (since some code fixers will change added templates)
            WellKnownStepIds.TemplateInserterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,

            // runs before default config/source updater so they won't have warnings about WCF
            WellKnownStepIds.ConfigUpdaterStepId,

            WellKnownStepIds.SourceUpdaterStepId
        };

        public WCFUpdateStep(ILogger<WCFUpdateStep> logger, ILoggerFactory loggerFactory)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _loggerFactory = loggerFactory;
            _path = new FilePath();
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

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

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var result = Apply();
                await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);
                return result.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Unexpected error happened during CoreWCF porting.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log file for more information.");
            }
        }

        public Task<UpgradeStepInitializeResult> Initialize(IProject project)
        {
            FindPath(project);
            if (_path.MainFile is null || _path.ProjectFile is null || _path.Config is null)
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

            Logger.LogInformation("This project is applicable for updating to CoreWCF. Initializing the update step...");

            string step = string.Empty;
            try
            {
                // construct updaters
                step = "creating configuration updater from this path:" + _path.Config;
                _configUpdater = UpdaterFactory.GetConfigUpdater(_path.Config, _loggerFactory.CreateLogger<ConfigUpdater>());
                step = "creating project file updater from this path:" + _path.ProjectFile;
                _packageUpdater = UpdaterFactory.GetPackageUpdater(_path.ProjectFile, _loggerFactory.CreateLogger<PackageUpdater>());
                if (_path.DirectiveFiles is not null)
                {
                    step = "creating using directivies updaters for .cs files that reference System.ServiceModel";
                    _directiveUpdaters = UpdaterFactory.GetDirectiveUpdaters(_path.DirectiveFiles, _loggerFactory.CreateLogger<SourceCodeUpdater>());
                }

                if (_configUpdater is not null)
                {
                    step = "retrieving project contexts from the configuration updater";
                    var configContext = new ConfigContext(_configUpdater);
                    step = "creating source code updater from this path:" + _path.MainFile;
                    _sourceCodeUpdater = UpdaterFactory.GetSourceCodeUpdater(_path.MainFile, configContext, _loggerFactory.CreateLogger<SourceCodeUpdater>());
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Unexpected error happened when trying to execute: {step}. Please review error message and log.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Updaters cannot be constructed. Please review error message and log for more information.", BuildBreakRisk.Medium));
            }

            Logger.LogInformation("Updaters are successfully constructed. Ready to start update.");
            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "WCF updaters are initialized. Ready to update.", BuildBreakRisk.Medium));
        }

        public Task<UpgradeStepApplyResult> Apply()
        {
            // set variable to provide information for potential errors
            string step = string.Empty;
            try
            {
                // Run updates
                step = "project file updates";
                var projFile = UpdateRunner.PackageUpdate(_packageUpdater!, Logger);
                step = "configuration file updates";
                var config = UpdateRunner.ConfigUpdate(_configUpdater!, Logger);
                step = "source code updates to replace ServiceHost";
                var source = UpdateRunner.SourceCodeUpdate(_sourceCodeUpdater!, Logger);
                var directives = new List<SyntaxNode>();
                if (_directiveUpdaters is not null)
                {
                    step = "using directivies updates for .cs files that references System.ServiceModel";
                    directives = UpdateRunner.UsingDirectivesUpdate(_directiveUpdaters, Logger);
                }

                // Write updates
                if (_path.MainFile is null || _path.ProjectFile is null || _path.Config is null)
                {
                    return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Lost access to file paths. Please review error message and log for more information."));
                }

                step = "writing project file updates to path:" + _path.ProjectFile;
                UpdateRunner.WritePackageUpdate(projFile, _path.ProjectFile, Logger);
                step = "writing configuration file updates to path:" + _path.Config;
                UpdateRunner.WriteConfigUpdate(config[0], config[1], _path.Config, Logger);
                step = "writing source code updates to path:" + _path.MainFile;
                UpdateRunner.WriteSourceCodeUpdate(source, _path.MainFile, Logger);
                if (directives.Count > 0 && _path.DirectiveFiles is not null)
                {
                    step = "writing using directives updates to .cs files that references System.ServiceModel";
                    UpdateRunner.WriteUsingDirectivesUpdate(directives, _path.DirectiveFiles, Logger);
                }

                Logger.LogInformation("Project was successfully updated to use CoreWCF services. Please review changes.");
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project was successfully ported to CoreWCF."));
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"Unexpected error happened when trying to execute: {step}. Please review error message and log.");
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Project failed to port to CoreWCF. Please review error message and log for more information."));
            }
        }

        private void FindPath(IProject project)
        {
            var compileItems = project.FindFiles(".cs", ProjectItemType.Compile);
            var configItems = project.FindFiles(".config", ProjectItemType.None);
            var distinct = new HashSet<string>();
            var serviceHostDetected = false;
            var directives = new List<string>();
            var config = new List<string>();
            var main = new List<string>();

            foreach (var item in compileItems)
            {
                var path = PathHelpers.GetNativePath(item);

                // Note: on macOS, we'll sometimes get duplicate items because one will be from an explicit
                // <Compile Include="Properties\AssemblyInfo.cs"/> which will use \'s in the path and the
                // other will be an implicit <Compile Include="**/*.cs"/> which will obviously use /'s.
                if (distinct.Add(path) && File.Exists(path))
                {
                    var text = File.ReadAllText(path);

                    if (text.Replace(" ", string.Empty).IndexOf("Main(", StringComparison.Ordinal) >= 0)
                    {
                        if (main.Count == 0 && text.IndexOf("ServiceHost", StringComparison.Ordinal) >= 0)
                        {
                            serviceHostDetected = true;
                        }

                        main.Add(path);
                    }
                    else if (text.IndexOf("using System.ServiceModel", StringComparison.Ordinal) >= 0)
                    {
                        directives.Add(path);
                    }
                }
            }

            distinct.Clear();
            foreach (var item in configItems)
            {
                var path = PathHelpers.GetNativePath(item);

                // Note: on macOS, we'll sometimes get duplicate items because one will be from an explicit
                // <None Include=.../> which will use \'s in the path and the other will be an implicit
                // <None Include="**/*.config"/> which will obviously use /'s.
                if (distinct.Add(path) && File.Exists(path))
                {
                    var text = File.ReadAllText(path);

                    if (text.IndexOf("<system.serviceModel>", StringComparison.Ordinal) >= 0)
                    {
                        config.Add(path);
                    }
                }
            }

            if (main.Count == 0)
            {
                Logger.LogWarning("Can not find .cs file with Main() method. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else if (main.Count > 1)
            {
                Logger.LogWarning("Found more than one .cs file with Main() method. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else if (config.Count == 0)
            {
                if (serviceHostDetected)
                {
                    Logger.LogWarning("ServiceHost instance was detected in code but can not find .config file that configures system.serviceModel. " +
                        "Automated update cannot be applied. Please update the project to CoreWCF manually (https://github.com/CoreWCF/CoreWCF).");
                }

                Logger.LogWarning("Did not find .config file that configures system.serviceModel. The project is not applicable for automated WCF update. No more work needs to be done and this step is complete.");
            }
            else
            {
                _path.MainFile = main[0];
                Logger.LogTrace($"This following file: {main[0]} needs source code update to replace ServiceHost instance.");
                _path.Config = config[0];
                Logger.LogTrace($"This following config file: {config[0]} needs to be updated.");
                _path.ProjectFile = project.GetFile().FilePath;
                Logger.LogTrace($"This following project file: {project.GetFile().FilePath} needs to be updated");

                // only updates the namespace for .cs files does not include clients
                var usingDirectivesUpdates = new List<string>();
                foreach (var directive in directives)
                {
                    var root = CSharpSyntaxTree.ParseText(File.ReadAllText(directive)).GetRoot();
                    if (ContainsIdentifier(root, "ChannelFactory") || ContainsIdentifier(root, "ClientBase"))
                    {
                        Logger.LogDebug($"This .cs file: {directive} does not need using directives updates.");
                    }
                    else
                    {
                        usingDirectivesUpdates.Add(directive);
                        Logger.LogDebug($"This .cs file: {directive} needs using directives updates. Adding the path to collection.");
                    }
                }

                if (usingDirectivesUpdates.Any())
                {
                    _path.DirectiveFiles = usingDirectivesUpdates;
                }

                Logger.LogDebug("Retrieved file paths that are needed for updates.");
            }
        }

        // Checks if the root has descendant nodes that contains id
        private static bool ContainsIdentifier(SyntaxNode root, string id)
        {
            return root.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.ValueText.IndexOf(id, StringComparison.Ordinal) >= 0);
        }
    }
}
