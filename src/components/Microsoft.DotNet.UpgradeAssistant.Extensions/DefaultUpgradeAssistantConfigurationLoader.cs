// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class DefaultUpgradeAssistantConfigurationLoader : IUpgradeAssistantConfigurationLoader
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
        };

        private readonly string _path;
        private readonly ILogger<DefaultUpgradeAssistantConfigurationLoader> _logger;

        public DefaultUpgradeAssistantConfigurationLoader(
            IOptions<ExtensionOptions> options,
            ILogger<DefaultUpgradeAssistantConfigurationLoader> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _path = options.Value.ConfigurationFilePath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UpgradeAssistantConfiguration> LoadAsync(CancellationToken token)
        {
            if (File.Exists(_path))
            {
                using var stream = File.OpenRead(_path);

                try
                {
                    var data = await JsonSerializer.DeserializeAsync<UpgradeAssistantConfiguration>(stream, _options, token).ConfigureAwait(false);

                    if (data is not null)
                    {
                        return data;
                    }
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Unexpected error reading configuration file at {Path}", _path);
                }
            }

            return new();
        }

        public async Task SaveAsync(UpgradeAssistantConfiguration configuration, CancellationToken token)
        {
            using var stream = File.OpenWrite(_path);

            await JsonSerializer.SerializeAsync(stream, configuration, _options, token).ConfigureAwait(false);
        }
    }
}
