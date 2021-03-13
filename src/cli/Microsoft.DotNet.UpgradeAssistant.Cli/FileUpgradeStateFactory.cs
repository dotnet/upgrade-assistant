// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class FileUpgradeStateFactory : IUpgradeStateManager
    {
        private readonly string _path;
        private readonly ILogger<FileUpgradeStateFactory> _logger;

        public FileUpgradeStateFactory(UpgradeOptions options, ILogger<FileUpgradeStateFactory> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _path = Path.Combine(options.Project.DirectoryName!, ".upgrade-assistant");
            _logger = logger;
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
                context.SetEntryPoint(FindProject(state.EntryPoint));
                context.SetCurrentProject(FindProject(state.CurrentProject));
            }

            IProject? FindProject(string? path)
                => path is null ? null : context.Projects.FirstOrDefault(p => NormalizePath(p.FilePath) == path);
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

            _logger.LogInformation("Saving upgrade progress file at {Path}", _path);

            using var stream = File.OpenWrite(_path);
            stream.SetLength(0);

            var state = new UpgradeState
            {
                EntryPoint = NormalizePath(context.EntryPoint?.FilePath),
                CurrentProject = NormalizePath(context.CurrentProject?.FilePath),
            };

            await JsonSerializer.SerializeAsync(stream, state, cancellationToken: token).ConfigureAwait(false);
        }

        private static string NormalizePath(string? path) => path is null ? string.Empty : Path.GetFileName(path);

        private class UpgradeState
        {
            public string Build { get; set; } = Constants.Version;

            public string? CurrentProject { get; set; }

            public string? EntryPoint { get; set; }

            public bool IsComplete { get; set; }
        }
    }
}
