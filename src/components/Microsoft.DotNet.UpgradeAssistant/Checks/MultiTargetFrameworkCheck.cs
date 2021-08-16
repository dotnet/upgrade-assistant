// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
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
    public class MultiTargetFrameworkCheck : IUpgradeReadyCheck
    {
        private const string FEATURE_LINK = "https://github.com/dotnet/upgrade-assistant/issues/640";

        private readonly ILogger<MultiTargetFrameworkCheck> _logger;

        public MultiTargetFrameworkCheck(ILogger<MultiTargetFrameworkCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => nameof(MultiTargetFrameworkCheck);

        public string UpgradeMessage => string.Format(CultureInfo.InvariantCulture, "Please see {0} to request this feature.", FEATURE_LINK);

        public async Task<UpgradeReadiness> IsReadyAsync(IProject project, UpgradeReadinessOptions options, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfms = project.TargetFrameworks;
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            if (tfms.Count == 1)
            {
                _logger.LogTrace("Confirmed project {Project} has a valid TFM ({TFM})", project.FileInfo, tfms.First());
                return UpgradeReadiness.Ready;
            }
            else
            {
                if (components.HasFlag(ProjectComponents.Maui))
                {
                    _logger.LogTrace("Confirmed project {Project} is of type .NET MAUI Head with TFMs : ({TFM}))", project.FileInfo, tfms.First());
                    return UpgradeReadiness.NotReady;
                }
                else
                {
                    _logger.LogError("Project {Project} cannot be upgraded. Input projects must have exactly one target framework. Please see {FeatureLink} to request this feature.", project.FileInfo, FEATURE_LINK);
                    return UpgradeReadiness.NotReady;
                }
            }
        }
    }
}
