// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public class VisualBasicWpfCheck : IUpgradeReadyCheck
    {
        private readonly ILogger<MultiTargetingCheck> _logger;

        public VisualBasicWpfCheck(ILogger<MultiTargetingCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => nameof(VisualBasicWpfCheck);

        public Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.Language == Language.VisualBasic && project.Components.HasFlag(ProjectComponents.Wpf))
            {
                _logger.LogCritical("Project {Project} cannot be upgraded. try-convert version 0.7.212201 does not support the migration of Visual Basic WPF applications", project.FilePath);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
