// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
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

        public UpgradeAssistantConfiguration Load()
        {
            if (File.Exists(_path))
            {
                var bytes = File.ReadAllBytes(_path);

                try
                {
                    var data = JsonSerializer.Deserialize<UpgradeAssistantConfiguration>(bytes, _options);

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

        public void Save(UpgradeAssistantConfiguration configuration)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(configuration, _options);
            File.WriteAllBytes(_path, bytes);
        }
    }
}
