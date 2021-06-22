// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class FileUpgradeStateFactory : IUpgradeStateManager
    {
        private readonly string _path;
        private readonly ILogger<FileUpgradeStateFactory> _logger;

        public FileUpgradeStateFactory(
            UpgradeOptions options,
            ILogger<FileUpgradeStateFactory> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _path = Path.Combine(options.Project.DirectoryName!, ".upgrade-assistant");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadStateAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var state = await GetStateAsync(token).ConfigureAwait(false);

            if (state is not null)
            {
                context.EntryPoints = state.EntryPoints.Select(e => FindProject(e)).Where(e => e != null)!;
                context.SetCurrentProject(FindProject(state.CurrentProject));
                foreach (var item in state.Properties)
                {
                    context.Properties.SetPropertyValue(item.Key, item.Value, true);
                }
            }

            IProject? FindProject(string? path)
                => path is null ? null : context.Projects.FirstOrDefault(p => NormalizePath(p.FileInfo.FullName) == path);
        }

        private async ValueTask<UpgradeState?> GetStateAsync(CancellationToken token)
        {
            if (File.Exists(_path))
            {
                _logger.LogInformation("Loading upgrade progress file at {Path}", _path);

                using var stream = File.OpenRead(_path);

                try
                {
                    var result = await JsonSerializer.DeserializeAsync<UpgradeState>(stream, cancellationToken: token).ConfigureAwait(false);

                    if (result is not null)
                    {
                        return result;
                    }

                    _logger.LogWarning("Contents of state file were empty.");
                }
                catch (JsonException e)
                {
                    _logger.LogWarning(e, "Could not deserialize upgrade progress.");
                }
            }

            return null;
        }

        public async Task SaveStateAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var state = new UpgradeState
            {
                EntryPoints = context.EntryPoints.Select(e => NormalizePath(e.FileInfo)),
                CurrentProject = NormalizePath(context.CurrentProject?.FileInfo),
                Properties = context.Properties.GetPersistentPropertyValues().ToDictionary(k => k.Key, v => v.Value)
            };

            if (state is { Empty: true })
            {
                _logger.LogInformation("No state to save");
            }
            else if (context.IsComplete)
            {
                try
                {
                    // When the .NET Upgrade Assistant is done with all steps,
                    // meaning all projects have been upgraded - delete the state.
                    if (File.Exists(_path))
                    {
                        _logger.LogInformation("Deleting upgrade progress file at {Path}", _path);
                        File.Delete(_path);
                    }
                }
                catch (Exception ex)
                when (ex is IOException or UnauthorizedAccessException or PathTooLongException)
                {
                    _logger.LogError("Unable to delete: {Path}, {Error}", _path, ex);
                }
            }
            else
            {
                _logger.LogInformation("Saving upgrade progress file at {Path}", _path);

                using var stream = File.OpenWrite(_path);
                stream.SetLength(0);

                await JsonSerializer.SerializeAsync(stream, state, cancellationToken: token).ConfigureAwait(false);
            }
        }

        private static string NormalizePath(FileInfo? file) => file is null ? string.Empty : file.Name;

        private static string NormalizePath(string? path) => path is null ? string.Empty : Path.GetFileName(path);

        private class UpgradeState
        {
            public string Build { get; init; } = UpgradeVersion.Current.FullVersion;

            public string? CurrentProject { get; init; }

            public IEnumerable<string> EntryPoints { get; init; } = Enumerable.Empty<string>();

            public Dictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

            [JsonIgnore]
            public bool Empty =>
                CurrentProject is null or { Length: 0 } &&
                !EntryPoints.Any() &&
                Properties is { Count: 0 };
        }
    }
}
