// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on ASP.NET projects with Web Forms
    /// should be informed that this is an unsupported technology before they
    /// invest significant time running the tool.
    /// </summary>
    public class WebFormsCheck : BypassableReadinessCheck
    {
        // NOTE: Intentionally does not detect user controls. https://docs.microsoft.com/en-us/previous-versions/aspnet/fb3w5b53(v=vs.100)
        // NOTE: Intentionally does not detect custom controls. A custom control is a class that you write that derives from Control or WebControl.
        // NOTE: Intentionally does not detect master pages. The assumption is that a project will also contain ASPX Web Forms if it contains master pages, user controls, or custom controls.
        private const string WebForms = "Web Forms";

        public WebFormsCheck(ILogger<WebFormsCheck> logger, UpgradeOptions upgradeOptions)
            : base(logger, upgradeOptions)
        {
        }

        /// <summary>
        /// Gets the value WebFormsCheck.
        /// </summary>
        public override string Id => nameof(WebFormsCheck);

        /// <summary>
        /// Gets a value indicating whether this check can be bypassed using the ReadinessAcknowledgement option.
        /// </summary>
        public override bool IsBypassable => true;

        public override string TechnologyDetected => WebForms;

        // TODO: code review - do we expect people to copy and paste this? Maybe we need to make a friendly URL.
        public override string SupportLink => "https://docs.microsoft.com/en-us/dotnet/architecture/porting-existing-aspnet-apps/migrate-web-forms";

        protected override async Task<bool> DoesProjectContainTechnologyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                return false;
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (!components.HasFlag(ProjectComponents.AspNet))
            {
                // this readiness check only applies to ASP.NET projects
                return false;
            }

            // NOTE: class libraries containing custom controls are NOT expected to trigger this check
            // This ReadyCheck should highlight the technology in the "head" project. Once the ReadinessAcknowledgement has been given it will bypass all remaining readiness checks.
            return DoesProjectContainWebFormsFiles(project);
        }

        private static bool DoesProjectContainWebFormsFiles(IProject project)
        {
            return project.FindFiles(".aspx").Any();
        }
    }
}
