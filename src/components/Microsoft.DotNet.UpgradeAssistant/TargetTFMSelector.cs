// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class TargetTFMSelector : ITargetTFMSelector
    {
        private const string NetStandardTFM = "netstandard2.0";
        private const string DefaultCurrentTFMBase = "net5.0";
        private const string DefaultLTSTFMBase = "net5.0";
        private const string WindowsSuffix = "-windows";

        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ILogger<TargetTFMSelector> _logger;
        private readonly string _currentTFMBase;
        private readonly string _ltsTFMBase;
        private readonly UpgradeTarget _upgradeTarget;

        private string AppTFMBase => _upgradeTarget == UpgradeTarget.Current ? _currentTFMBase : _ltsTFMBase;

        public TargetTFMSelector(UpgradeOptions options, IOptions<TFMSelectorOptions> selectorOptions, ITargetFrameworkMonikerComparer tfmComparer, ILogger<TargetTFMSelector> logger)
        {
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentTFMBase = selectorOptions?.Value.CurrentTFMBase ?? DefaultCurrentTFMBase;
            _ltsTFMBase = selectorOptions?.Value.LTSTFMBase ?? DefaultLTSTFMBase;
            _upgradeTarget = options?.UpgradeTarget ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Chooses the most likely target TFM a project should be retargeted to based on its style, output type, dependencies, and
        /// the user's preference of current or LTS.
        /// </summary>
        public async ValueTask<TargetFrameworkMoniker> SelectTargetFrameworkAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfmName = GetNetStandardTFM(project);
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            // Projects with web components or an Exe output type should use app TFMs
            if (components.HasFlag(ProjectComponents.AspNet) || components.HasFlag(ProjectComponents.AspNetCore) || project.OutputType == ProjectOutputType.Exe)
            {
                tfmName = AppTFMBase;
            }

            // Projects with Windows Desktop components or a WinExe output type should use a -windows suffix
            if (components.HasFlag(ProjectComponents.WindowsDesktop) || project.OutputType == ProjectOutputType.WinExe)
            {
                tfmName = $"{AppTFMBase}{WindowsSuffix}";

                if (components.HasFlag(ProjectComponents.WinRT))
                {
                    // TODO: Default to this version to ensure everything is supported.
                    tfmName += "10.0.19041.0";
                }
            }

            _logger.LogDebug("Considering TFM {TFM} for project {Project} based on its style and output type ({ProjectStyle}, {ProjectOutputType})", tfmName, project.FileInfo, components, project.OutputType);

            var tfm = EnsureProjectDependenciesNoDowngrade(tfmName, project);

            _logger.LogDebug("Recommending TFM {TFM} for project {Project}", tfm, project.FileInfo);

            // Ensure we don't downgrade a project
            return EnsureNoDowngrade(tfm, project.TargetFrameworks);
        }

        public TargetFrameworkMoniker EnsureProjectDependenciesNoDowngrade(string tfmName, IProject project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfm = new TargetFrameworkMoniker(tfmName);

            foreach (var dep in project.ProjectReferences)
            {
                tfm = EnsureNoDowngrade(tfm, dep.TargetFrameworks);
            }

            return tfm;
        }

        private TargetFrameworkMoniker EnsureNoDowngrade(TargetFrameworkMoniker tfm, IEnumerable<TargetFrameworkMoniker> others)
        {
            var min = others.OrderBy(t => t, _tfmComparer)
                .Where(tfm => !tfm.IsFramework)
                .FirstOrDefault();

            if (min is not null)
            {
                if (_tfmComparer.Compare(min, tfm) > 0)
                {
                    tfm = min;
                }
            }

            return tfm;
        }

        private static string GetNetStandardTFM(IProject project)
        {
            foreach (var currentTfm in project.TargetFrameworks)
            {
                if (currentTfm.IsNetStandard)
                {
                    return currentTfm.Name;
                }
            }

            return NetStandardTFM;
        }
    }
}
