// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on projects that build WCF Services
    /// should be informed that this is an unsupported technology before they
    /// invest significant time running the tool.
    /// </summary>
    public class WcfServerCheck : BypassableReadinessCheck
    {
        public WcfServerCheck(ILogger<WcfServerCheck> logger, UpgradeOptions upgradeOptions)
            : base(logger, upgradeOptions)
        {
        }

        /// <summary>
        /// Gets the value WebFormsCheck.
        /// </summary>
        public override string Id => nameof(WcfServerCheck);

        /// <summary>
        /// Gets a value indicating whether this check can be bypassed using the ReadinessAcknowledgement option.
        /// </summary>
        public override bool IsBypassable => true;

        public override string TechnologyDetected => "WCF Server-side Services";

        // TODO: code review - do we expect people to copy and paste this? Maybe we need to make a friendly URL.
        public override string SupportLink => "https://docs.microsoft.com/en-us/dotnet/architecture/porting-existing-aspnet-apps/migrate-web-forms";

        protected override async Task<bool> DoesProjectContainTechnologyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                // this is not a valid scenario
                // satisfies CA1062
                return false;
            }

            // are there any svc.cs files in this project?
            return project.FindFiles($".svc{GetExtensionForLanguage(project)}").Any();
        }

        private static string GetExtensionForLanguage(IProject project)
        {
            if (project.Language == Language.VisualBasic)
            {
                return ".vb";
            }

            return ".cs";
        }
    }
}
