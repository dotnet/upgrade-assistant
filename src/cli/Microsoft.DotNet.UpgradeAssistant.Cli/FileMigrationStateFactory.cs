using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class FileMigrationStateFactory : IMigrationStateManager
    {
        private readonly string _path;
        private readonly ILogger<FileMigrationStateFactory> _logger;

        public FileMigrationStateFactory(MigrateOptions options, ILogger<FileMigrationStateFactory> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _path = Path.Combine(options.Project.DirectoryName!, ".upgrade-assistant");
            _logger = logger;
        }

        public async Task LoadStateAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var state = await GetStateAsync(token).ConfigureAwait(false);

            context.SetEntryPoint(FindProject(state.EntryPoint));
            context.SetCurrentProject(FindProject(state.CurrentProject));
            context.IsComplete = state.IsComplete;

            IProject? FindProject(string? path)
                => path is null ? null : context.Projects.FirstOrDefault(p => NormalizePath(p.FilePath) == path);
        }

        private async ValueTask<MigrationState> GetStateAsync(CancellationToken token)
        {
            if (File.Exists(_path))
            {
                _logger.LogInformation("Loading migration progress file at {Path}", _path);

                using var stream = File.OpenRead(_path);

                try
                {
                    var result = await JsonSerializer.DeserializeAsync<MigrationState>(stream, cancellationToken: token).ConfigureAwait(false);

                    if (result is not null)
                    {
                        return result;
                    }

                    _logger.LogWarning("Contents of state file were empty.");
                }
                catch (JsonException e)
                {
                    _logger.LogWarning(e, "Could not deserialize migration progress.");
                }
            }

            return new MigrationState();
        }

        public async Task SaveStateAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogInformation("Saving migration progress file at {Path}", _path);

            using var stream = File.OpenWrite(_path);
            stream.SetLength(0);

            var state = new MigrationState
            {
                EntryPoint = NormalizePath(context.EntryPoint?.Project.FilePath),
                CurrentProject = NormalizePath(context.CurrentProject?.Project.FilePath),
                IsComplete = context.IsComplete,
            };

            await JsonSerializer.SerializeAsync(stream, state, cancellationToken: token).ConfigureAwait(false);
        }

        private static string NormalizePath(string? path) => path is null ? string.Empty : Path.GetFileName(path);

        private class MigrationState
        {
            public string Build { get; set; } = Constants.Version;

            public string? CurrentProject { get; set; }

            public string? EntryPoint { get; set; }

            public bool IsComplete { get; set; }
        }
    }
}
