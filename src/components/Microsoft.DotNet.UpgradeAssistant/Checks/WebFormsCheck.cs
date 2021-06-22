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
    /// Users running upgrade-assistant on ASP.NET projects with Web Forms
    /// should be informed that this is an unsupported technology before they
    /// invest significant time running the tool.
    /// </summary>
    public class WebFormsCheck : IUpgradeReadyCheck
    {
        private const string CATEGORY = "Web Forms";
        private const string UPGRADE_LINK = "https://aka.ms/migrate-web-forms";
        private const string WEB_FORMS_MESSAGE = "Support for {0} is limited to .NET Full Framework. To learn more please read: {1}";

        private readonly ILogger<WebFormsCheck> _logger;

        public WebFormsCheck(ILogger<WebFormsCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the value WebFormsCheck.
        /// </summary>
        public string Id => nameof(WebFormsCheck);

        // NOTE: Intentionally does not detect user controls. https://docs.microsoft.com/en-us/previous-versions/aspnet/fb3w5b53(v=vs.100)
        // NOTE: Intentionally does not detect custom controls. A custom control is a class that you write that derives from Control or WebControl.
        // NOTE: Intentionally does not detect master pages. The assumption is that a project will also contain ASPX Web Forms if it contains master pages, user controls, or custom controls.
        public string UpgradeMessage => string.Format(CultureInfo.InvariantCulture, WEB_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);

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

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (options.IgnoreUnsupportedFeatures || !components.HasFlag(ProjectComponents.AspNet))
            {
                // this readiness check only applies to ASP.NET projects
                return UpgradeReadiness.Ready;
            }

            // NOTE: class libraries containing custom controls are NOT expected to trigger this check
            // This ReadyCheck should highlight the technology in the "head" project.
            if (DoesProjectContainWebFormsFiles(project))
            {
                _logger.LogError(WEB_FORMS_MESSAGE, CATEGORY, UPGRADE_LINK);
                return UpgradeReadiness.Unsupported;
            }

            return UpgradeReadiness.Ready;
        }

        private static bool DoesProjectContainWebFormsFiles(IProject project)
        {
            return project.FindFiles(".aspx").Any();
        }
    }
}
