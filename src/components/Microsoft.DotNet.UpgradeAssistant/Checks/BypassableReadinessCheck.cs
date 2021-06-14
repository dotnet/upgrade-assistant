// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public abstract class BypassableReadinessCheck : IUpgradeReadyCheck
    {
        private readonly ILogger _logger;
        private readonly UpgradeOptions _upgradeOptions;

        protected BypassableReadinessCheck(ILogger logger, UpgradeOptions upgradeOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _upgradeOptions = upgradeOptions ?? throw new ArgumentNullException(nameof(upgradeOptions));
        }

        public abstract string Id { get; }

        public abstract bool IsBypassable { get; }

        public abstract string TechnologyDetected { get; }

        /// <summary>
        /// When relevant, the ReadinessCheck should proactively share guidance for migrating technologies that are not supported on .NET latest.
        /// </summary>
        public abstract string SupportLink { get; }

        protected abstract Task<bool> DoesProjectContainTechnologyAsync(IProject project, CancellationToken token);

        public async Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                return false;
            }

            if (_upgradeOptions.ReadinessAcknowledgement)
            {
                // the user has bypassed this constraint (opted out of this feature)
                return true;
            }

            if (await DoesProjectContainTechnologyAsync(project, token).ConfigureAwait(false))
            {
                _logger.LogError("Project {Name} uses the {TechnologyDetected} which is not supported.", project.FileInfo, TechnologyDetected);
                _logger.LogError("   To learn more about what you can do we recommend: {SupportLink}", SupportLink);
                _logger.LogError("If you would like to upgrade this project you can restart upgrade-assistant with the \"--readiness-acknowledgement\' option.");

                return false;
            }

            return true;
        }
    }
}
