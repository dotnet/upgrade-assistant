// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on ASP.NET projects with VBHTML
    /// templates should be informed that this is an unsupported technology
    /// before they invest significant time running the tool.
    /// </summary>
    public class VisualBasicRazorTemplateCheck : IUpgradeReadyCheck
    {
        /// <summary>
        ///  Gets the value VisualBasicRazorTemplateCheck.
        /// </summary>
        public string Id => nameof(VisualBasicRazorTemplateCheck);

        public string UpgradeGuidance => "https://devblogs.microsoft.com/vbteam/combining-angular-visual-basic-and-net-core-for-developing-modern-web-apps/";

        public async Task<UpgradeReadiness> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.Language != Language.VisualBasic)
            {
                // this readiness check only applies to Visual Basic projects
                return UpgradeReadiness.Ready;
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (!components.HasFlag(ProjectComponents.AspNet) && !components.HasFlag(ProjectComponents.AspNetCore))
            {
                // this readiness check only applies to ASP.NET projects
                return UpgradeReadiness.Ready;
            }

            return DoesProjectContainRazorFiles(project) ? UpgradeReadiness.Unsupported : UpgradeReadiness.Ready;
        }

        private static bool DoesProjectContainRazorFiles(IProject project)
        {
            return project.FindFiles(".vbhtml").Any();
        }
    }
}
