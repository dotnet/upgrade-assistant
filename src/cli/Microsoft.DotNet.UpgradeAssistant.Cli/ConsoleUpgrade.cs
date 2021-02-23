// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Upgrader;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleUpgrade : IAppCommand
    {
        private readonly IUserInput _input;
        private readonly InputOutputStreams _io;
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly CommandProvider _commandProvider;
        private readonly UpgraderManager _migrator;
        private readonly IUpgradeStateManager _stateManager;
        private readonly ILogger<ConsoleUpgrade> _logger;

        public ConsoleUpgrade(
            IUserInput input,
            InputOutputStreams io,
            IUpgradeContextFactory contextFactory,
            CommandProvider commandProvider,
            UpgraderManager migratorManager,
            IUpgradeStateManager stateManager,
            ILogger<ConsoleUpgrade> logger)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
            _migrator = migratorManager ?? throw new ArgumentNullException(nameof(migratorManager));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(CancellationToken token)
        {
            using var context = await _contextFactory.CreateContext(token);

            await _stateManager.LoadStateAsync(context, token);

            try
            {
                // Cache current steps here as defense-in-depth against the possibility
                // of a bug (or very weird migration step behavior) causing the current step
                // to reset state after being initialized by GetNextStepAsync
                var steps = await _migrator.InitializeAsync(context, token);
                var step = await _migrator.GetNextStepAsync(context, token);

                while (step is not null)
                {
                    while (!step.IsDone)
                    {
                        token.ThrowIfCancellationRequested();

                        ShowUpgradeSteps(steps, context, step);
                        _io.Output.WriteLine();

                        var commands = _commandProvider.GetCommands(step);
                        var command = await _input.ChooseAsync("Choose a command:", commands, token);

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
                        else if (await _input.WaitToProceedAsync(token))
                        {
                            ConsoleUtils.Clear();
                        }
                        else
                        {
                            _logger.LogWarning("Upgrade process was canceled. Quitting....");
                            return;
                        }
                    }

                    step = await _migrator.GetNextStepAsync(context, token);
                }

                _logger.LogInformation("Upgrade has completed. Please review any changes.");
            }
            finally
            {
                // Do not pass the same token as it may have been canceled and we still need to persist this.
                await _stateManager.SaveStateAsync(context, default);
            }
        }

        private void ShowUpgradeSteps(IEnumerable<UpgradeStep> steps, IUpgradeContext context, UpgradeStep? currentStep = null, int offset = 0)
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
                _io.Output.WriteLine("Upgrade Steps");
                _io.Output.WriteLine();

                var displayedProjectInfo = false;

                if (context.EntryPoint is not null)
                {
                    _io.Output.WriteLine($"Entrypoint: {context.EntryPoint.FilePath}");
                    displayedProjectInfo = true;
                }

                if (context.CurrentProject is not null)
                {
                    _io.Output.WriteLine($"Current Project: {context.CurrentProject.FilePath}");
                    displayedProjectInfo = true;
                }

                if (displayedProjectInfo)
                {
                    _io.Output.WriteLine();
                }
            }

            foreach (var step in steps)
            {
                // Write indent (if any) and item number
                var indexStr = offset % 2 == 0
                    ? $"{count++}. "
                    : $"{(char)('a' + (count++ - 1))}. ";
                _io.Output.Write($"{new string(' ', offset * 4)}{indexStr}");

                // Write the step title and make a note of whether the step is incomplete
                // (since that would mean future steps shouldn't show "[Next step]")
                WriteStepStatus(step, step == currentStep);
                _io.Output.WriteLine(step.Title);
                nextStepFound = nextStepFound || step.Status != UpgradeStepStatus.Complete;

                ShowUpgradeSteps(step.SubSteps, context, currentStep, offset + 1);
            }
        }

        private void WriteStepStatus(UpgradeStep step, bool isNextStep)
        {
            switch (step.Status)
            {
                case UpgradeStepStatus.Complete:
                    Console.ForegroundColor = ConsoleColor.Green;
                    _io.Output.Write("[Complete] ");
                    break;
                case UpgradeStepStatus.Failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    _io.Output.Write("[Failed] ");
                    break;
                case UpgradeStepStatus.Skipped:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    _io.Output.Write("[Skipped] ");
                    break;
                case UpgradeStepStatus.Incomplete:
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
