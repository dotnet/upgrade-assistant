using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator;
using AspNetMigrator.ConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests
{
    public static class MigrationRunner
    {
        public static async Task MigrateAsync(string inputPath, TextWriter output, IEnumerable<string> commands, int timeoutSeconds = 300)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentException($"'{nameof(inputPath)}' cannot be null or empty.", nameof(inputPath));
            }

            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (commands is null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            var project = new FileInfo(inputPath);
            using var cts = new CancellationTokenSource();

            var migrationTask = Program.RunMigrationAsync(new MigrateOptions { SkipBackup = true, Project = project }, (context, services) => RegisterInputOutput(context, services, output, commands), cts.Token);
            var timeoutTimer = Task.Delay(timeoutSeconds * 1000, cts.Token);

            await Task.WhenAny(migrationTask, timeoutTimer).ConfigureAwait(false);
            cts.Cancel();

            try
            {
                await Task.WhenAll(migrationTask, timeoutTimer).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void RegisterInputOutput(HostBuilderContext context, IServiceCollection services, TextWriter output, IEnumerable<string> commands)
        {
            var servicesToRemove = services.Where(sd => sd.ServiceType.Equals(typeof(InputOutputStreams))).ToArray();
            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            services.AddSingleton(new InputOutputStreams(new StringReader(string.Join('\n', commands)), output));
        }
    }
}
