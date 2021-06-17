// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on ASP.NET projects with Web Forms
    /// should be informed that this is an unsupported technology before they
    /// invest significant time running the tool.
    /// </summary>
    public class WebFormsCheck : IUpgradeReadyCheck
    {
        /// <summary>
        /// Gets the value WebFormsCheck.
        /// </summary>
        public string Id => nameof(WebFormsCheck);

        // NOTE: Intentionally does not detect user controls. https://docs.microsoft.com/en-us/previous-versions/aspnet/fb3w5b53(v=vs.100)
        // NOTE: Intentionally does not detect custom controls. A custom control is a class that you write that derives from Control or WebControl.
        // NOTE: Intentionally does not detect master pages. The assumption is that a project will also contain ASPX Web Forms if it contains master pages, user controls, or custom controls.
        public string UpgradeGuidance => $"Support for Web Forms is limited to .NET Full Framework. To learn more please read: https://aka.ms/migrate-web-forms";

        public async Task<UpgradeReadiness> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (!components.HasFlag(ProjectComponents.AspNet))
            {
                // this readiness check only applies to ASP.NET projects
                return UpgradeReadiness.Ready;
            }

            // NOTE: class libraries containing custom controls are NOT expected to trigger this check
            // This ReadyCheck should highlight the technology in the "head" project.
            return DoesProjectContainWebFormsFiles(project) ? UpgradeReadiness.Unsupported : UpgradeReadiness.Ready;
        }

        private static bool DoesProjectContainWebFormsFiles(IProject project)
        {
            return project.FindFiles(".aspx").Any();
        }
    }
}
