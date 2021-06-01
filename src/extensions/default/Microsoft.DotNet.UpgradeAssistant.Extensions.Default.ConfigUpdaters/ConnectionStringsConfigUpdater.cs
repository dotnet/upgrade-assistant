// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters
{
    public class ConnectionStringsConfigUpdater : IUpdater<ConfigFile>
    {
        private const string ConnectionStringsPath = "/configuration/connectionStrings";
        private const string AddConnectionStringElementName = "add";
        private const string KeyAttributeName = "name";
        private const string ValueAttributeName = "connectionString";
        private const string AppSettingsJsonFileName = "appsettings.json";
        private const string ConnectionStringsObjectName = "ConnectionStrings";

        private static readonly Regex AppSettingsFileRegex = new("^appsettings(\\..+)?\\.json$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            // Determine where appsettings.json should live
            var appSettingsPath = Path.Combine(project.FileInfo.DirectoryName ?? string.Empty, AppSettingsJsonFileName);

            // Parse existing appsettings.json properties, if any
            var existingJson = "{}";
            if (File.Exists(appSettingsPath))
            {
                // Read all text instead of keeping the stream open so that we can
                // re-open the config file later in this method as writeable
                existingJson = File.ReadAllText(appSettingsPath);
            }

            using var json = JsonDocument.Parse(existingJson, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var existingProperties = json.RootElement.EnumerateObject();

            // Write an updated appsettings.json file including the previous properties and new ones for the new connection string properties
            using var fs = new FileStream(appSettingsPath, FileMode.Create, FileAccess.Write);
            using var jsonWriter = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
            {
                jsonWriter.WriteStartObject();
                foreach (var property in existingProperties)
                {
                    property.WriteTo(jsonWriter);
                }

                // Write new object for the connection strings
                jsonWriter.WriteStartObject(ConnectionStringsObjectName);
                foreach (var setting in _connectionStrings)
                {
                    jsonWriter.WriteString(setting.Key, setting.Value);
                }

                // Close the ConnectionStrings object
                jsonWriter.WriteEndObject();

                jsonWriter.WriteEndObject();
            }

            // Confirm that the appsettings.json file is included in the project. In rare cases (auto-include disabled),
            // it may be necessary to add it explicitly
            var file = project.GetFile();

            if (!file.ContainsItem(appSettingsPath, ProjectItemType.Content, token))
            {
                // Remove the directory that was added at the beginning of this method.
                var itemPath = Path.IsPathRooted(appSettingsPath)
                    ? Path.GetFileName(appSettingsPath)
                    : appSettingsPath;
                file.AddItem(ProjectItemType.Content.Name, itemPath);
                await file.SaveAsync(token).ConfigureAwait(false);
            }

            return new DefaultUpdaterResult(true);
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
                            var key = connectionString.Attribute(KeyAttributeName);
                            var value = connectionString.Attribute(ValueAttributeName);
                            if (key is not null && value is not null)
                            {
                                _logger.LogDebug("Found connection string {ConnectionStringName} in {ConfigFilePath}", key.Value, configFile.Path);
                                connectionStrings[key.Value] = value.Value;
                            }
                        }
                    }
                }
            }

            var project = context.CurrentProject.Required();

            var jsonConfigFiles = project.FindFiles(ProjectItemType.Content, AppSettingsFileRegex)
                .Select(f => new AppSettingsFile(f))
                .ToList();

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

            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(result));
        }
    }
}
