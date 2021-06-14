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

        public override string SupportLink => "https://docs.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/migrate-wcf-to-grpc";

        protected override Task<bool> DoesProjectContainTechnologyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                return Task.FromResult(false);
            }

            // are there any svc.cs files in this project?
            return Task.FromResult(project.FindFiles($".svc{GetExtensionForLanguage(project)}").Any());
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
