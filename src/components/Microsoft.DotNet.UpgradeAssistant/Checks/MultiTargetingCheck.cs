// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public class MultiTargetingCheck : IUpgradeReadyCheck
    {
        private readonly ILogger<MultiTargetingCheck> _logger;

        public MultiTargetingCheck(ILogger<MultiTargetingCheck> logger)
        {
            _logger = logger;
        }

        public string Id => nameof(MultiTargetingCheck);

        public Task<bool> IsReadyAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var project in context.Projects)
            {
                var projectRoot = project.GetFile();

                try
                {
                    var tfm = project.TFM;
                    _logger.LogTrace("Confirmed project {Project} has a valid TFM ({TFM})", project.FilePath, tfm);
                }
                catch (UpgradeException)
                {
                    _logger.LogCritical("Project {Project} cannot be upgraded. Input projects must have exactly one <TargetFramework> or <TargetFrameworkVersion> property. Multi-targeted projects are not yet supported.", project.FilePath);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
    }
}
