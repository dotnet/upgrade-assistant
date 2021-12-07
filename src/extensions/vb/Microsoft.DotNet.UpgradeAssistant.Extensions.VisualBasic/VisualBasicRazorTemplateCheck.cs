// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    /// <summary>
    /// Users running upgrade-assistant on ASP.NET projects with VBHTML
    /// templates should be informed that this is an unsupported technology
    /// before they invest significant time running the tool.
    /// </summary>
    public class VisualBasicRazorTemplateCheck : IUpgradeReadyCheck
    {
        private const string CATEGORY = "the VB Razor engine";
        private const string UPGRADE_LINK = "https://aka.ms/vb-angular-and-web-api";
        private const string VBHTML_MESSAGE = $"Support for {CATEGORY} is limited to .NET Full Framework. To learn more please read: {UPGRADE_LINK}";

        private readonly ILogger<VisualBasicRazorTemplateCheck> _logger;

        public VisualBasicRazorTemplateCheck(ILogger<VisualBasicRazorTemplateCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///  Gets the value VisualBasicRazorTemplateCheck.
        /// </summary>
        public string Id => nameof(VisualBasicRazorTemplateCheck);

        public string UpgradeMessage => VBHTML_MESSAGE;

        public async Task<UpgradeReadiness> IsReadyAsync(IProject project, UpgradeReadinessOptions options, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.IgnoreUnsupportedFeatures || project.Language != Language.VisualBasic)
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

            if (DoesProjectContainRazorFiles(project))
            {
                _logger.LogError(VBHTML_MESSAGE);
                return UpgradeReadiness.Unsupported;
            }

            return UpgradeReadiness.Ready;
        }

        private static bool DoesProjectContainRazorFiles(IProject project)
        {
            return project.FindFiles(".vbhtml").Any();
        }
    }
}
