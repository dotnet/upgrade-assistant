// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Checks
{
    public class CanLoadProjectFile : IUpgradeReadyCheck
    {
        private readonly ILogger<CanLoadProjectFile> _logger;

        public CanLoadProjectFile(ILogger<CanLoadProjectFile> logger)
        {
            _logger = logger;
        }

        public string Id => nameof(CanLoadProjectFile);

        public Task<bool> IsReadyAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            try
            {
                // Just need to access some values in the file to see if it can be properly loaded.
                _ = project.GetFile().GetPropertyValue("SomeValue");
                return Task.FromResult(true);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError("Project {Name} can not be loaded: {Message}", project.FilePath, e.Message);
                return Task.FromResult(false);
            }
        }
    }
}
