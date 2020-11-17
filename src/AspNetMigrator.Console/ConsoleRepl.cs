using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using AspNetMigrator.Engine.GlobalCommands;
using AspNetMigrator.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetMigrator.ConsoleApp
{
    // TODO : Eventually, this may need localized and pull strings from resources, etc.
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "The prototype is not yet localized")]
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No sync context in console apps")]
    public class ConsoleRepl : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        // the REPL will loop while !_done
        private bool _done;

        public ConsoleRepl(ILogger logger, IServiceProvider services, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
            _services = services;
        }

        public async Task StartAsync(CancellationToken token)
        {
            if (await RunStartupTasks(token))
            {
                await RunRepl(token);
            }
            else
            {
                _logger.Error("Error encountered while starting migration");
            }

            _lifetime.StopApplication();
        }

        private async Task<bool> RunStartupTasks(CancellationToken token)
        {
            var startupTasks = _services.GetRequiredService<IEnumerable<IMigrationStartup>>()
                         .Select(m => m.StartupAsync(token));
            var completion = await Task.WhenAll(startupTasks);

            return completion.All(t => t);
        }

        private async Task RunRepl(CancellationToken token)
        {
            using var scope = _services.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<MigrateOptions>();
            var migrator = scope.ServiceProvider.GetRequiredService<Migrator>();
            var handlerFactory = scope.ServiceProvider.GetRequiredService<CommandResultHandlerFactory>();

            using var context = new MSBuildWorkspaceMigrationContext(options.ProjectPath)
            {
                Migrator = migrator,
            };

            await migrator.InitializeAsync(context, token);

            while (!_done)
            {
                ShowMigrationSteps(migrator.Steps);

                var command = GetCommand(migrator);
                var commandResultHandler = handlerFactory.GetHandler(command?.GetType());
                var commandResult = await command.ExecuteAsync(context, token);
                commandResultHandler.HandleResult(commandResult);
            }

            _lifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        private void SetProgramIsDone()
        {
            _done = true;
        }

        private MigrationCommand GetCommand(Migrator migrator)
        {
            var listOfCommands = GlobalCommands.GetCommands(SeeMoreDetailsCommandResultHandler.SendAllMessagesToUserAsync, SetProgramIsDone);
            if (migrator?.NextStep?.Commands != null)
            {
                listOfCommands.InsertRange(0, migrator.NextStep.Commands);
            }

            Console.WriteLine("Choose command");
            for (var i = 0; i < listOfCommands.Count; i++)
            {
                Console.WriteLine($" {i + 1}. {listOfCommands[i].CommandText}");
            }

            Console.Write("> ");

            var result = Console.ReadLine();

            if (result is null)
            {
                return new ExitCommand(SetProgramIsDone);
            }

            var selectedCommandText = result.Trim(' ', '.', '\t');
            if (int.TryParse(selectedCommandText, out int selectedCommandIndex))
            {
                selectedCommandIndex--;
                if (selectedCommandIndex >= 0 && selectedCommandIndex < listOfCommands.Count)
                {
                    return listOfCommands[selectedCommandIndex];
                }
            }

            return new UnknownCommand();
        }

        private static void ShowMigrationSteps(IEnumerable<MigrationStep> steps, int offset = 0)
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
                WriteStepStatus(step, !nextStepFound);
                Console.WriteLine(step.Title);
                nextStepFound = nextStepFound || (step.Status != MigrationStepStatus.Complete);

                ShowMigrationSteps(step.SubSteps, offset + 1);
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
