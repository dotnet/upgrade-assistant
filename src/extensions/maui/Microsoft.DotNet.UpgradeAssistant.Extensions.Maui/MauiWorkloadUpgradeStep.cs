// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiWorkloadUpgradeStep : UpgradeStep
    {
        private static readonly IReadOnlyDictionary<string, ProjectComponents> MauiWorkloadMap = new Dictionary<string, ProjectComponents>
        {
            { "maui-android", ProjectComponents.MauiAndroid },
            { "maui-ios", ProjectComponents.MauiiOS },
            { "maui", ProjectComponents.MauiAndroid | ProjectComponents.MauiiOS },
        };

        private static bool? _infoSucceeded;
        private static bool? _installSucceeded;
        private readonly IProcessRunner _runner;

        public override string Title => "Install .NET MAUI Workload";

        public override string Description => "Check the .NET SDK for the MAUI workload and install it if necessary.";

        public MauiWorkloadUpgradeStep(ILogger<MauiWorkloadUpgradeStep> logger, IProcessRunner runner)
            : base(logger)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.BackupStepId,
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            typeof(XamlNamespaceUpgradeStep).FullName,
        };

        // Install or update the MAUI workload
        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_installSucceeded.HasValue)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, ".NET MAUI workload install has already been run");
            }

            _installSucceeded = await RunDotnetCommandAsync(context, "workload install maui", (_, message) => LogLevel.Information, token).ConfigureAwait(false);

            if (!_installSucceeded.Value)
            {
                Logger.LogError("Command 'dotnet workload install maui' failed!");

                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, ".NET MAUI workload installation failed!");
            }

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $".NET MAUI workload installation succeeded.");
        }

        // Check if the right MAUI workload is installed
        protected async override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_installSucceeded.HasValue)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, ".NET MAUI workload install has already been run", _installSucceeded.Value ? BuildBreakRisk.None : BuildBreakRisk.High);
            }

            var project = context.CurrentProject.Required();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            var workloads = ProjectComponents.None;

            if (!_infoSucceeded.HasValue)
            {
                // We only need to display the dotnet info debug information once
                _infoSucceeded = await RunDotnetCommandAsync(context, "--info", (_, message) => LogLevel.Debug, token).ConfigureAwait(false);
            }

            var result = await RunDotnetCommandAsync(context, "workload list", (_, message) =>
                {
                    var workload = message.Split(' ').First();
                    if (MauiWorkloadMap.TryGetValue(workload, out var component))
                    {
                        workloads |= component;
                    }

                    return LogLevel.Information;
                }, token).ConfigureAwait(false);

            if (!result)
            {
                Logger.LogError("Failed to run 'dotnet workload install maui' command!");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Failed to list .NET MAUI workloads", BuildBreakRisk.High);
            }
            else if (workloads.HasFlag(components & (ProjectComponents.MauiAndroid | ProjectComponents.MauiiOS)))
            {
                Logger.LogInformation($".NET MAUI workloads installed: {workloads}");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, ".NET MAUI workload(s) already installed", BuildBreakRisk.None);
            }
            else
            {
                Logger.LogInformation(".NET MAUI workload needs to be installed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, ".NET MAUI workload needs to be installed", BuildBreakRisk.High);
            }
        }

        // Check if this is a MAUI conversion
        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return false;
            }

            var project = context.CurrentProject.Required();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            if (components.HasFlag(ProjectComponents.MauiAndroid) || components.HasFlag(ProjectComponents.MauiiOS) || components.HasFlag(ProjectComponents.Maui))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(context.Properties.GetPropertyValue("componentFlag")))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Run specified `dotnet workload` command.
        /// </summary>
        public Task<bool> RunDotnetCommandAsync(IUpgradeContext context, string command, Func<bool, string, LogLevel> getMessageLogLevel, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _runner.RunProcessAsync(new ProcessInfo
            {
                Command = "dotnet",
                Arguments = command,
                EnvironmentVariables = context.GlobalProperties,
                Name = "dotnet",
                GetMessageLogLevel = getMessageLogLevel,
            }, token);
        }
    }
}
