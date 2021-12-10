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

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Updaters
{
    public abstract class BaseAppSettingsConfigUpdater
    {
        private const string RuleId = "UA201";
        private const string AppSettingsJsonFileName = "appsettings.json";

        private static readonly Regex AppSettingsFileRegex = new("^appsettings(\\..+)?\\.json$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Method to override to write changes to the appsettings.json file.
        /// </summary>
        /// <param name="jsonWriter">appsettings.json writer.</param>
        public abstract void WriteChangesToAppSettings(Utf8JsonWriter jsonWriter);

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

            // Write an updated appsettings.json file including the previous properties and new ones for the new app settings properties
            using var fs = new FileStream(appSettingsPath, FileMode.Create, FileAccess.Write);
            using var jsonWriter = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
            {
                jsonWriter.WriteStartObject();
                foreach (var property in existingProperties)
                {
                    property.WriteTo(jsonWriter);
                }

                WriteChangesToAppSettings(jsonWriter);

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

            return new DefaultUpdaterResult(
                RuleId,
                RuleName: string.Empty,
                FullDescription: string.Empty,
                true);
        }

        public static IList<AppSettingsFile> FindExistingAppSettingsFiles(IUpgradeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var jsonConfigFiles = project.FindFiles(AppSettingsFileRegex, ProjectItemType.Content)
                .Select(f => new AppSettingsFile(f))
                .ToList();

            return jsonConfigFiles;
        }
    }
}
