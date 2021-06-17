// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Checks to see if the project has more than one target framework moniker.
    /// Multi-target projects are not currently supported.
    /// </summary>
    public class TargetFrameworkCheck : IUpgradeReadyCheck
    {
        private readonly ILogger<TargetFrameworkCheck> _logger;

        public TargetFrameworkCheck(ILogger<TargetFrameworkCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => nameof(TargetFrameworkCheck);

        public string UpgradeGuidance => "Please see https://github.com/dotnet/upgrade-assistant/issues/252 to request this feature.";

        public Task<UpgradeReadiness> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfms = project.TargetFrameworks;

            if (tfms.Count == 1)
            {
                _logger.LogTrace("Confirmed project {Project} has a valid TFM ({TFM})", project.FileInfo, tfms.First());
                return Task.FromResult(UpgradeReadiness.Ready);
            }
            else
            {
                _logger.LogError("Project {Project} cannot be upgraded. Input projects must have exactly one target framework. {UpgradeGuidance}", project.FileInfo, UpgradeGuidance);
                return Task.FromResult(UpgradeReadiness.NotReady);
            }
        }
    }
}
