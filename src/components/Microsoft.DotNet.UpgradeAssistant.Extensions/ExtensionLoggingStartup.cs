// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                _logger.LogDebug("Loaded extension: {Name} [{Location}]", extension.Name, extension.Location);
            }

            return Task.FromResult(true);
        }
    }
}
