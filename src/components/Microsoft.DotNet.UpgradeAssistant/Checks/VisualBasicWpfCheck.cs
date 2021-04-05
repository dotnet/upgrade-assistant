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
        private readonly ILogger<TargetFrameworkCheck> _logger;

        public VisualBasicWpfCheck(ILogger<TargetFrameworkCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => nameof(VisualBasicWpfCheck);

        public async Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            if (project.Language == Language.VisualBasic && components.HasFlag(ProjectComponents.Wpf))
            {
                _logger.LogError("Project {Project} cannot be upgraded. try-convert version 0.7.212201 does not support the migration of Visual Basic WPF applications", project.FileInfo);
                return false;
            }

            return true;
        }
    }
}
