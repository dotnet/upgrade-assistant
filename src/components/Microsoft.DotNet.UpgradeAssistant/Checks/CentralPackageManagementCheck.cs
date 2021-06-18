// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public class CentralPackageManagementCheck : IUpgradeReadyCheck
    {
        private const string FEATURE_LINK = "https://github.com/dotnet/upgrade-assistant/issues/252";

        private readonly ILogger<CentralPackageManagementCheck> _logger;

        public CentralPackageManagementCheck(ILogger<CentralPackageManagementCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => nameof(CentralPackageManagementCheck);

        public string UpgradeMessage => string.Format(CultureInfo.InvariantCulture, "Please see {0} to request this feature.", FEATURE_LINK);

        public Task<UpgradeReadiness> IsReadyAsync(IProject project, UpgradeReadinessOptions options, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var projectRoot = project.GetFile();

            if (bool.TryParse(projectRoot.GetPropertyValue("EnableCentralPackageVersions"), out var result) && result)
            {
                _logger.LogError("Project {Name} uses EnableCentralPackageVersions which is not currently supported. Please see {FeatureLink} to request this feature.", project.FileInfo, FEATURE_LINK);
                return Task.FromResult(UpgradeReadiness.NotReady);
            }
            else
            {
                return Task.FromResult(UpgradeReadiness.Ready);
            }
        }
    }
}
