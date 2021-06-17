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
        /// Gets the value 'WebFormsCheck'.
        /// </summary>
        public string Id => nameof(WcfServerCheck);

        public string UpgradeGuidance => "https://docs.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/migrate-wcf-to-grpc";

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
