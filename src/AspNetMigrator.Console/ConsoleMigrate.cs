﻿using System;
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
    // TODO : Eventually, this may need to be localized and pull strings from resources, etc.
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No sync context in console apps")]
    public class ConsoleMigrate : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ICollectUserInput _input;
        private readonly InputOutputStreams _io;
        private readonly CommandProvider _commandProvider;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConsoleMigrate(
            ICollectUserInput input,
            InputOutputStreams io,
            CommandProvider commandProvider,
            ILogger<ConsoleMigrate> logger,
            IServiceProvider services,
            IHostApplicationLifetime lifetime)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                _logger.LogInformation("Configuration loaded from context base directory: {BaseDirectory}", AppContext.BaseDirectory);

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
            catch (MigrationException e)
            {
                _logger.LogError("Unexpected error: {Message}", e.Message);
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
            var contextFactory = scope.ServiceProvider.GetRequiredService<IMigrationContextFactory>();
            var context = await contextFactory.CreateContext(token);
            var options = scope.ServiceProvider.GetRequiredService<MigrateOptions>();
            var migrator = scope.ServiceProvider.GetRequiredService<Migrator>();
            var stateManager = scope.ServiceProvider.GetRequiredService<IMigrationStateManager>();

            await stateManager.LoadStateAsync(context, token);

            try
            {
                // Cache current steps here as defense-in-depth against the possibility
                // of a bug (or very weird migration step behavior) causing the current step
                // to reset state after being initialized by GetNextStepAsync
                var steps = migrator.GetStepsForContext(context);
                var step = await migrator.GetNextStepAsync(context, token);

                while (step is not null)
                {
                    while (!step.IsDone)
                    {
                        token.ThrowIfCancellationRequested();

                        ShowMigrationSteps(steps, step);

                        var commands = _commandProvider.GetCommands(step);
                        var command = await _input.ChooseAsync("Choose command", commands, token);

                        // TODO : It might be nice to allow commands to show more details by having a 'status' property
                        //        that can be shown here. Also, commands currently only return bools but, in the future,
                        //        if they return more complex objects, custom handlers could be used to respond to the different
                        //        commands' return values.
                        if (!await command.ExecuteAsync(context, token))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            _io.Output.WriteLine($"Command ({command.CommandText}) did not succeed");
                            Console.ResetColor();
                        }
                    }

                    step = await migrator.GetNextStepAsync(context, token);
                }

                // Once a project has completed, we can remove it from the context.
                await context.SetProjectAsync(null, token);

                _logger.LogInformation("Migration has completed. Please review any changes.");
            }
            finally
            {
                // Do not pass the same token as it may have been canceled and we still need to persist this.
                await stateManager.SaveStateAsync(context, default);
            }
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        private void ShowMigrationSteps(IEnumerable<MigrationStep> steps, MigrationStep? currentStep = null, int offset = 0)
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
                _io.Output.WriteLine();
                _io.Output.WriteLine("Migration Steps");
            }

            foreach (var step in steps)
            {
                // Write indent (if any) and item number
                _io.Output.Write($"{new string(' ', offset * 4)}{count++}. ");

                // Write the step title and make a note of whether the step is incomplete
                // (since that would mean future steps shouldn't show "[Next step]")
                WriteStepStatus(step, step == currentStep);
                _io.Output.WriteLine(step.Title);
                nextStepFound = nextStepFound || (step.Status != MigrationStepStatus.Complete);

                ShowMigrationSteps(step.SubSteps, currentStep, offset + 1);
            }
        }

        private void WriteStepStatus(MigrationStep step, bool isNextStep)
        {
            switch (step.Status)
            {
                case MigrationStepStatus.Complete:
                    Console.ForegroundColor = ConsoleColor.Green;
                    _io.Output.Write("[Complete] ");
                    break;
                case MigrationStepStatus.Failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    _io.Output.Write("[Failed] ");
                    break;
                case MigrationStepStatus.Skipped:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    _io.Output.Write("[Skipped] ");
                    break;
                case MigrationStepStatus.Incomplete:
                    if (isNextStep)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        _io.Output.Write("[Next step] ");
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
