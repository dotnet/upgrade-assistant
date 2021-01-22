using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator;
using AspNetMigrator.ConsoleApp;
using AspNetMigrator.PackageUpdater;
using Microsoft.Extensions.DependencyInjection;

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

            var migrationTask = Program.RunCommandAsync(new MigrateOptions { SkipBackup = true, Project = project }, (context, services) => RegisterTestServices(services, output, commands), AppCommand.Migrate, cts.Token);
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

        private static void RegisterTestServices(IServiceCollection services, TextWriter output, IEnumerable<string> commands)
        {
            var servicesToRemove = services.Where(sd => sd.ServiceType.Equals(typeof(InputOutputStreams))).ToArray();
            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            services.AddSingleton(new InputOutputStreams(new StringReader(string.Join('\n', commands)), output));
            services.AddOptions<PackageUpdaterStepOptions>().Configure(o =>
            {
                o.LogRestoreOutput = false;
                o.PackageMapPath = "PackageMaps";
                o.MigrationAnalyzersPackageSource = "https://doesnotexist.net/index.json";
                o.MigrationAnalyzersPackageVersion = "1.0.0";
            });
        }
    }
}
