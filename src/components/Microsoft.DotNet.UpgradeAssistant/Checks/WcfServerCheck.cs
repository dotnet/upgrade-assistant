// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    /// <summary>
    /// Users running upgrade-assistant on projects that build WCF Services
    /// should be informed that this is an unsupported technology before they
    /// invest significant time running the tool.
    /// </summary>
    public class WcfServerCheck : IUpgradeReadyCheck
    {
        /// <summary>
        /// Gets the value 'WcfServerCheck'.
        /// </summary>
        public string Id => nameof(WcfServerCheck);

        // todo: code review - make new aka.ms link waiting on feedback from the team
        public string UpgradeGuidance => $"Support for WCF Server-side Services is limited to .NET Full Framework. To learn more please read: https://aka.ms/migrate-wcf-to-grpc";

        public Task<UpgradeReadiness> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            // are there any svc.cs files in this project?
            if (project.FindFiles($".svc{GetExtensionForLanguage(project)}").Any())
            {
                return Task.FromResult(UpgradeReadiness.Unsupported);
            }

            return Task.FromResult(UpgradeReadiness.Ready);
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
