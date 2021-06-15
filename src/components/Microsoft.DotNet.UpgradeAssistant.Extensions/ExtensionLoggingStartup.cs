// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionLoggingStartup : IUpgradeStartup
    {
        private readonly ILogger<ExtensionLoggingStartup> _logger;
        private readonly IEnumerable<ExtensionInstance> _extensions;

        public ExtensionLoggingStartup(ILogger<ExtensionLoggingStartup> logger, IEnumerable<ExtensionInstance> extensions)
        {
            _logger = logger;
            _extensions = extensions;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            _logger.LogInformation("Loaded {0} extensions", _extensions.Count());

            foreach (var extension in _extensions)
            {
                if (extension.Version is Version version)
                {
                    _logger.LogDebug("Loaded extension: {Name} v{Version} [{Location}]", extension.Name, version, extension.Location);
                }
                else
                {
                    _logger.LogDebug("Loaded extension: {Name} [{Location}]", extension.Name, extension.Location);
                }
            }

            return Task.FromResult(true);
        }
    }
}
