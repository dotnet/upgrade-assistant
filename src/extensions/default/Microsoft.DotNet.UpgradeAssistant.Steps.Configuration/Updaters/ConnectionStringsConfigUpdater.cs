// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Updaters
{
    public class ConnectionStringsConfigUpdater : BaseAppSettingsConfigUpdater, IUpdater<ConfigFile>
    {
        private const string RuleId = "UA204";
        private const string ConnectionStringsPath = "/configuration/connectionStrings";
        private const string AddConnectionStringElementName = "add";
        private const string NameAttributeName = "name";
        private const string ConnectionStringAttributeName = "connectionString";
        private const string ConnectionStringsObjectName = "ConnectionStrings";

        private readonly ILogger<ConnectionStringsConfigUpdater> _logger;
        private readonly Dictionary<string, string> _connectionStrings;

        public string Id => typeof(ConnectionStringsConfigUpdater).FullName!;

        public string Title => "Convert Connection Strings";

        public string Description => "Convert connection strings from app.config and web.config files to appsettings.json";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public ConnectionStringsConfigUpdater(ILogger<ConnectionStringsConfigUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStrings = new Dictionary<string, string>();
        }

        public override void WriteChangesToAppSettings(Utf8JsonWriter jsonWriter)
        {
            if (jsonWriter is null)
            {
                throw new ArgumentNullException(nameof(jsonWriter));
            }

            // Write new object for the connection strings
            jsonWriter.WriteStartObject(ConnectionStringsObjectName);
            foreach (var setting in _connectionStrings)
            {
                jsonWriter.WriteString(setting.Key, setting.Value);
            }

            // Close the ConnectionStrings object
            jsonWriter.WriteEndObject();
        }

        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Find connectionStrings elements in the config files
            var connectionStrings = new Dictionary<string, string>();
            foreach (var configFile in inputs)
            {
                var connectionStringsElement = configFile.Contents.XPathSelectElement(ConnectionStringsPath);
                if (connectionStringsElement is not null)
                {
                    foreach (var connectionString in connectionStringsElement.Elements(AddConnectionStringElementName))
                    {
                        if (connectionString is not null)
                        {
                            var key = connectionString.Attribute(NameAttributeName);
                            var value = connectionString.Attribute(ConnectionStringAttributeName);
                            if (key is not null && value is not null)
                            {
                                _logger.LogDebug("Found connection string {ConnectionStringName} in {ConfigFilePath}", key.Value, configFile.Path);
                                connectionStrings[key.Value] = value.Value;
                            }
                        }
                    }
                }
            }

            var jsonConfigFiles = FindExistingAppSettingsFiles(context);

            // Check for existing appSettings.json files for connection strings
            foreach (var setting in connectionStrings)
            {
                if (!jsonConfigFiles.Any(s => !string.IsNullOrEmpty(s.Configuration.GetConnectionString(setting.Key))))
                {
                    _connectionStrings[setting.Key] = setting.Value;
                }
                else
                {
                    _logger.LogDebug("Existing app settings already include connection string {ConnectionStringName}", setting.Key);
                }
            }

            _logger.LogInformation("Found {ConnectionStringsCount} connection strings for upgrade: {ConnectionStringNames}", _connectionStrings.Count, string.Join(", ", _connectionStrings.Keys));

            var result = _connectionStrings.Count > 0;

            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(
                RuleId,
                RuleName: Id,
                FullDescription: Title,
                result));
        }
    }
}
