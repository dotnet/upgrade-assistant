// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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

        public Task<bool> IsReadyAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projects = context.Projects
                .Where(p => bool.TryParse(p.GetFile().GetPropertyValue("EnableCentralPackageVersions"), out var result) ? result : false)
                .ToList();

            if (!projects.Any())
            {
                return Task.FromResult(true);
            }

            foreach (var project in projects)
            {
                _logger.LogCritical("Project {Name} uses EnableCentralPackageVersions which is not currently supported. Please see https://github.com/dotnet/upgrade-assistant/issues/252 to request this feature.", project.FilePath);
            }

            return Task.FromResult(false);
        }
    }
}
