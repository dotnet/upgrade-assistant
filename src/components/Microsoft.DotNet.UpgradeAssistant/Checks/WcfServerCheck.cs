// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    public class WcfServerCheck : IUpgradeReadyCheck
    {
        private const string WCF_LINK = "https://aka.ms/CoreWCF/migration";
        private const string GRPC_LINK = "https://aka.ms/migrate-wcf-to-grpc";
        private const string CATEGORY = "WCF Server-side Services";
        private const string WCF_MESSAGE = $"Support for {CATEGORY} is limited to .NET Full Framework. Consider updating to use CoreWCF ({WCF_LINK}) in later provided steps or rewriting to use gRPC ({GRPC_LINK}).";
        private readonly ILogger<WcfServerCheck> _logger;

        public WcfServerCheck(ILogger<WcfServerCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the value 'WcfServerCheck'.
        /// </summary>
        public string Id => nameof(WcfServerCheck);

        public string UpgradeMessage => WCF_MESSAGE;

        public Task<UpgradeReadiness> IsReadyAsync(IProject project, UpgradeReadinessOptions options, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.IgnoreUnsupportedFeatures)
            {
                return Task.FromResult(UpgradeReadiness.Ready);
            }

            // are there any svc.cs files in this project?
            if (project.FindFiles($".svc{GetExtensionForLanguage(project)}").Any())
            {
                _logger.LogError(WCF_MESSAGE);
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
