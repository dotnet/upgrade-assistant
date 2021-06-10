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
    /// Customers running upgrade-assistant on ASP.NET projects with VBHTML
    /// templates should be informed that this is an unsupported technology
    /// before they invest significant time running the tool.
    /// </summary>
    public class VisualBasicRazorTemplateCheck : IUpgradeReadyCheck
    {
        private readonly ILogger<VisualBasicRazorTemplateCheck> _logger;
        private readonly UpgradeOptions _upgradeOptions;

        public VisualBasicRazorTemplateCheck(ILogger<VisualBasicRazorTemplateCheck> logger, UpgradeOptions upgradeOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _upgradeOptions = upgradeOptions ?? throw new ArgumentNullException(nameof(upgradeOptions));
        }

        /// <summary>
        /// VisualBasicRazorTemplateCheck.
        /// </summary>
        public string Id => nameof(VisualBasicRazorTemplateCheck);

        /// <summary>
        /// Gets a value indicating whether this check can be bypassed using the ReadinessAcknowledgement option.
        /// </summary>
        public bool IsBypassable => true;

        public string TechnologyDetected => "VB Razor Engine";

        // TODO: code review - do we expect people to copy and paste this? Maybe we need to make a friendly URL.
        public string SupportLink => "https://devblogs.microsoft.com/vbteam/combining-angular-visual-basic-and-net-core-for-developing-modern-web-apps/";

        public async Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                // this is not a valid scenario
                // satisfies CA1062
                return false;
            }

            if (_upgradeOptions.ReadinessAcknowledgement)
            {
                // the user has bypassed this constraint (opted out of this feature)
                return true;
            }

            if (project.Language != Language.VisualBasic)
            {
                // this readiness check only applies to Visual Basic projects
                return true;
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (!components.HasFlag(ProjectComponents.AspNet))
            {
                // this readiness check only applies to ASP.NET projects
                return true;
            }

            if (DoesProjectContainRazorFiles(project))
            {
                _logger.LogError("Project {Name} uses the {TechnologyDetected} which is not supported.", project.FileInfo, TechnologyDetected);
                _logger.LogError("   To learn more about what you can do we recommend: {SupportLink}", SupportLink);
                _logger.LogError("If you would like to upgrade this project you can restart upgrade-assistant with the \"--readiness-acknowledgement\' option.");

                return false;
            }

            return true;
        }

        private static bool DoesProjectContainRazorFiles(IProject project)
        {
            return project.FindFiles(".vbhtml").Any();
        }
    }
}
