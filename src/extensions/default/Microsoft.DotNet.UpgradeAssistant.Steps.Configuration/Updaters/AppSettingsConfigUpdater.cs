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
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Updaters
{
    public class AppSettingsConfigUpdater : BaseAppSettingsConfigUpdater, IUpdater<ConfigFile>
    {
        private const string AppSettingsPath = "/configuration/appSettings";
        private const string AddSettingElementName = "add";
        private const string KeyAttributeName = "key";
        private const string ValueAttributeName = "value";

        private readonly ILogger<AppSettingsConfigUpdater> _logger;
        private readonly Dictionary<string, string> _appSettings;

        public string Id => typeof(AppSettingsConfigUpdater).FullName!;

        public string Title => "Convert Application Settings";

        public string Description => "Convert application settings from app.config and web.config files to appsettings.json";

        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        public AppSettingsConfigUpdater(ILogger<AppSettingsConfigUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = new Dictionary<string, string>();
        }

        public override void WriteChangesToAppSettings(Utf8JsonWriter jsonWriter)
        {
            if (jsonWriter is null)
            {
                throw new ArgumentNullException(nameof(jsonWriter));
            }

            foreach (var setting in _appSettings)
            {
                if (bool.TryParse(setting.Value, out var boolValue))
                {
                    jsonWriter.WriteBoolean(setting.Key, boolValue);
                }
                else if (long.TryParse(setting.Value, out var longValue))
                {
                    jsonWriter.WriteNumber(setting.Key, longValue);
                }
                else if (double.TryParse(setting.Value, out var doubleValue))
                {
                    jsonWriter.WriteNumber(setting.Key, doubleValue);
                }
                else
                {
                    jsonWriter.WriteString(setting.Key, setting.Value);
                }
            }
        }

        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<ConfigFile> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Find appSettings elements in the config files
            var appSettings = new Dictionary<string, string>();
            foreach (var configFile in inputs)
            {
                var appSettingsElement = configFile.Contents.XPathSelectElement(AppSettingsPath);
                if (appSettingsElement is not null)
                {
                    foreach (var setting in appSettingsElement.Elements(AddSettingElementName))
                    {
                        if (setting is not null)
                        {
                            var key = setting.Attribute(KeyAttributeName);
                            var value = setting.Attribute(ValueAttributeName);
                            if (key is not null && value is not null)
                            {
                                _logger.LogDebug("Found app setting {AppSettingName} in {ConfigFilePath}", key.Value, configFile.Path);
                                appSettings[key.Value] = value.Value;
                            }
                        }
                    }
                }
            }

            var jsonConfigFiles = FindExistingAppSettingsFiles(context);

            // Check for existing appSettings.json files for app settings
            foreach (var setting in appSettings)
            {
                if (!jsonConfigFiles.Any(s => !string.IsNullOrEmpty(s.Configuration[setting.Key])))
                {
                    _appSettings[setting.Key] = setting.Value;
                }
                else
                {
                    _logger.LogDebug("Existing app settings already include setting {SettingName}", setting.Key);
                }
            }

            _logger.LogInformation("Found {AppSettingCount} app settings for upgrade: {AppSettingNames}", _appSettings.Count, string.Join(", ", _appSettings.Keys));

            var result = _appSettings.Count > 0;

            return Task.FromResult<IUpdaterResult>(new DefaultUpdaterResult(
                RuleId: "Id",
                RuleName: Id,
                FullDescription: Title,
                result));
        }
    }
}
