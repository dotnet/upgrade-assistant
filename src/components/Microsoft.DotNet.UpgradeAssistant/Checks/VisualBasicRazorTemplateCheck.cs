// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on ASP.NET projects with VBHTML
    /// templates should be informed that this is an unsupported technology
    /// before they invest significant time running the tool.
    /// </summary>
    public class VisualBasicRazorTemplateCheck : BypassableReadinessCheck
    {
        public VisualBasicRazorTemplateCheck(ILogger<VisualBasicRazorTemplateCheck> logger, UpgradeOptions upgradeOptions)
            : base(logger, upgradeOptions)
        {
        }

        /// <summary>
        ///  Gets the value VisualBasicRazorTemplateCheck.
        /// </summary>
        public override string Id => nameof(VisualBasicRazorTemplateCheck);

        /// <summary>
        /// Gets a value indicating whether this check can be bypassed using the ReadinessAcknowledgement option.
        /// </summary>
        public override bool IsBypassable => true;

        public override string TechnologyDetected => "VB Razor Engine";

        // TODO: code review - do we expect people to copy and paste this? Maybe we need to make a friendly URL.
        public override string SupportLink => "https://devblogs.microsoft.com/vbteam/combining-angular-visual-basic-and-net-core-for-developing-modern-web-apps/";

        protected override async Task<bool> DoesProjectContainTechnologyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                // this is not a valid scenario
                // satisfies CA1062
                return false;
            }

            if (project.Language != Language.VisualBasic)
            {
                // this readiness check only applies to Visual Basic projects
                return false;
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (!components.HasFlag(ProjectComponents.AspNet))
            {
                // this readiness check only applies to ASP.NET projects
                return false;
            }

            return DoesProjectContainRazorFiles(project);
        }

        private static bool DoesProjectContainRazorFiles(IProject project)
        {
            return project.FindFiles(".vbhtml").Any();
        }
    }
}
