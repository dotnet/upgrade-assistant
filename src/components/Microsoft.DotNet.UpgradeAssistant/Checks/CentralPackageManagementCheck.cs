// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public class CentralPackageManagementCheck : IUpgradeReadyCheck
    {
        private readonly ILogger<CentralPackageManagementCheck> _logger;

        public CentralPackageManagementCheck(ILogger<CentralPackageManagementCheck> logger)
        {
            _logger = logger;
        }

        public string Id => nameof(CentralPackageManagementCheck);

        public Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var projectRoot = project.GetFile();

            if (bool.TryParse(projectRoot.GetPropertyValue("EnableCentralPackageVersions"), out var result) && result)
            {
                _logger.LogError("Project {Name} uses EnableCentralPackageVersions which is not currently supported. Please see https://github.com/dotnet/upgrade-assistant/issues/252 to request this feature.", project.FilePath);
                return Task.FromResult(false);
            }
            else
            {
                return Task.FromResult(true);
            }
        }
    }
}
