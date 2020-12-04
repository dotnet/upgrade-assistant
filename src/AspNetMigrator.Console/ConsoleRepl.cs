using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp
{
    // TODO : Eventually, this may need localized and pull strings from resources, etc.
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No sync context in console apps")]
    public class ConsoleRepl : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ICollectUserInput _input;
        private readonly CommandProvider _commandProvider;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConsoleRepl(
            ICollectUserInput input,
            CommandProvider commandProvider,
            ILogger<ConsoleRepl> logger,
            IServiceProvider services,
            IHostApplicationLifetime lifetime)
        {
            _input = input;
            _commandProvider = commandProvider;
            _logger = logger;
            _lifetime = lifetime;
            _services = services;
        }

        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                if (await RunStartupTasks(token))
                {
                    await RunReplAsync(token);
                }
                else
                {
                    _logger.LogError("Error encountered while starting migration");
                }

                _lifetime.StopApplication();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        private async Task<bool> RunStartupTasks(CancellationToken token)
        {
            var startupTasks = _services.GetRequiredService<IEnumerable<IMigrationStartup>>()
                .Select(m => m.StartupAsync(token));
            var completion = await Task.WhenAll(startupTasks);

            return completion.All(t => t);
        }

        private async Task RunReplAsync(CancellationToken token)
        {
            using var scope = _services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<IMigrationContext>();
            var options = scope.ServiceProvider.GetRequiredService<MigrateOptions>();
            var migrator = scope.ServiceProvider.GetRequiredService<Migrator>();

            await foreach (var step in migrator.GetAllSteps(context, token))
            {
                while (!step.IsComplete)
                {
                    token.ThrowIfCancellationRequested();

                    ShowMigrationSteps(migrator.Steps, step);

                    var commands = _commandProvider.GetCommands(step);
                    var command = await _input.ChooseAsync("Choose command", commands, token);

                    // TODO : It might be nice to allow commands to show more details by having a 'status' property
                    //        that can be shown here. Also, commands currently only return bools but, in the future,
                    //        if they return more complex objects, custom handlers could be used to respond to the different
                    //        commands' return values.
                    if (!await command.ExecuteAsync(context, token))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Command ({command.CommandText}) did not succeed");
                        Console.ResetColor();
                    }
                }
            }

            ShowMigrationSteps(migrator.Steps);

            _logger.LogInformation("Migration has completed. Please review any changes.");
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        private static void ShowMigrationSteps(IEnumerable<MigrationStep> steps, MigrationStep? currentStep = null, int offset = 0)
        {
            if (steps is null || !steps.Any())
            {
                return;
            }

            Console.ResetColor();
            var nextStepFound = false;
            var count = 1;

            if (offset == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Migration Steps");
            }

            foreach (var step in steps)
            {
                // Write indent (if any) and item number
                Console.Write($"{new string(' ', offset * 2)}{count++}. ");

                // Write the step title and make a note of whether the step is incomplete
                // (since that would mean future steps shouldn't show "[Current step]")
                WriteStepStatus(step, step == currentStep);
                Console.WriteLine(step.Title);
                nextStepFound = nextStepFound || (step.Status != MigrationStepStatus.Complete);

                ShowMigrationSteps(step.SubSteps, currentStep, offset + 1);
            }

            Console.WriteLine();
        }

        private static void WriteStepStatus(MigrationStep step, bool isNextStep)
        {
            switch (step.Status)
            {
                case MigrationStepStatus.Complete:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[Complete] ");
                    break;
                case MigrationStepStatus.Failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[Failed] ");
                    break;
                case MigrationStepStatus.Skipped:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("[Skipped] ");
                    break;
                case MigrationStepStatus.Incomplete:
                    if (isNextStep)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("[Current step] ");
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
