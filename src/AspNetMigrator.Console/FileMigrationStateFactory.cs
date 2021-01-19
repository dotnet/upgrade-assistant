using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp
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

            var projectDirectory = Path.GetDirectoryName(options.ProjectPath)!;

            _path = Path.Combine(projectDirectory, ".aspnetmigrator");
            _logger = logger;
        }

        public async Task LoadStateAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var state = await GetStateAsync(token).ConfigureAwait(false);

            var project = await context.GetProjects(token).FirstOrDefaultAsync(p => p.GetRoslynProject().Name == state.CurrentProject, token).ConfigureAwait(false);

            if (project is not null)
            {
                context.SetProject(project);
            }
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

            var project = await context.GetProjectAsync(token).ConfigureAwait(false);

            var state = new MigrationState
            {
                CurrentProject = project?.GetRoslynProject().Name,
            };

            await JsonSerializer.SerializeAsync(stream, state, cancellationToken: token).ConfigureAwait(false);
        }

        private class MigrationState
        {
            public string Build { get; set; } = Constants.Version;

            public string? CurrentProject { get; set; }
        }
    }
}
