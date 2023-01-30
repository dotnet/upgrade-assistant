// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleUpgrade : IAppCommand
    {
        private readonly IUserInput _input;
        private readonly IUpgradeContextAccessor _context;
        private readonly InputOutputStreams _io;
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly CommandProvider _commandProvider;
        private readonly UpgraderManager _upgrader;
        private readonly ITelemetry _telemetry;
        private readonly IUpgradeStateManager _stateManager;
        private readonly ILogger<ConsoleUpgrade> _logger;
        private readonly IOutputResultWriterProvider _writerProvider;
        private readonly IOptions<OutputOptions> _options;

        public ConsoleUpgrade(
            IUserInput input,
            IUpgradeContextAccessor context,
            InputOutputStreams io,
            IUpgradeContextFactory contextFactory,
            CommandProvider commandProvider,
            UpgraderManager upgrader,
            ITelemetry telemetry,
            IUpgradeStateManager stateManager,
            ILogger<ConsoleUpgrade> logger,
            IOutputResultWriterProvider writerProvider,
            IOptions<OutputOptions> options)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _context = context;
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
            _upgrader = upgrader ?? throw new ArgumentNullException(nameof(upgrader));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _writerProvider = writerProvider ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(CancellationToken token)
        {
            using var context = await _contextFactory.CreateContext(token);

            _context.Current = context;

            await _stateManager.LoadStateAsync(context, token);
            if (!_writerProvider.TryGetWriter(_options.Value.Format, out var resultWriter))
            {
                _logger.LogError(LocalizedStrings.RequestedFormatUnavailableMessage, _options.Value.Format);
                return;
            }

            try
            {
                // Cache current steps here as defense-in-depth against the possibility
                // of a bug (or very weird upgrade step behavior) causing the current step
                // to reset state after being initialized by GetNextStepAsync
                var steps = await _upgrader.InitializeAsync(context, token);
                var step = await _upgrader.GetNextStepAsync(context, token);

                while (step is not null)
                {
                    await ShowUpgradeStepsAsync(steps, context, token, step);

                    await RunStepAsync(context, step, token);

                    step = await _upgrader.GetNextStepAsync(context, token);
                }

                _logger.LogInformation("Upgrade has completed. Please review any changes.");
            }
            finally
            {
                // Do not pass the same token as it may have been canceled and we still need to persist this.
                await _stateManager.SaveStateAsync(context, default);
                await WriteUpgradeReport(resultWriter, context, default);
            }
        }

        private async Task<bool> WriteUpgradeReport(IOutputResultWriter resultWriter, IUpgradeContext context, CancellationToken token)
        {
            var outputDirectory = Path.GetDirectoryName(context.InputPath);
            if (outputDirectory is null || !context.Results.Any())
            {
                return false;
            }

            var outputFileName = $"UpgradeReport.{_options.Value.Format}";
            var outputFilePath = Path.Combine(outputDirectory, outputFileName);
            using var stream = File.Create(Path.Combine(outputDirectory, outputFileName));
            await resultWriter.WriteAsync(context.Results.ToAsyncEnumerable(), stream, token);
            _logger.LogInformation($"The Upgrade Report is generated at {outputFilePath}");
            return true;
        }

        private async Task RunStepAsync(IUpgradeContext context, UpgradeStep step, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await _io.Output.WriteLineAsync();

            var commands = _commandProvider.GetCommands(step, context);
            var command = await _input.ChooseAsync("Choose a command:", commands, token);

            // TODO : It might be nice to allow commands to show more details by having a 'status' property
            //        that can be shown here. Also, commands currently only return bools but, in the future,
            //        if they return more complex objects, custom handlers could be used to respond to the different
            //        commands' return values.
            if (!await ExecuteAndTimeCommand(context, step, command, token))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                await _io.Output.WriteLineAsync($"Command ({command.CommandText}) did not succeed");
                Console.ResetColor();
            }
            else if (!await _input.WaitToProceedAsync(token))
            {
                _logger.LogWarning("Upgrade process was canceled. Quitting....");
            }

            token.ThrowIfCancellationRequested();
        }

        private async ValueTask<bool> ExecuteAndTimeCommand(IUpgradeContext context, UpgradeStep step, UpgradeCommand command, CancellationToken token)
        {
            using (_telemetry.TimeStep(command.Id, step))
            {
                return await command.ExecuteAsync(context, token);
            }
        }

        private async Task ShowUpgradeStepsAsync(IEnumerable<UpgradeStep> steps, IUpgradeContext context, CancellationToken token, UpgradeStep? currentStep = null, int offset = 0)
        {
            if (!_input.IsInteractive)
            {
                return;
            }

            if (steps is null || !steps.Any())
            {
                return;
            }

            Console.ResetColor();
            var nextStepFound = false;
            var count = 1;

            if (offset == 0)
            {
                await _io.Output.WriteLineAsync();
                await _io.Output.WriteLineAsync("Upgrade Steps");
                await _io.Output.WriteLineAsync();

                var displayedProjectInfo = false;

                foreach (var entrypoint in context.EntryPoints)
                {
                    await _io.Output.WriteLineAsync($"Entrypoint: {entrypoint.FileInfo}");
                    displayedProjectInfo = true;
                }

                if (context.CurrentProject is not null)
                {
                    await _io.Output.WriteLineAsync($"Current Project: {context.CurrentProject.FileInfo}");
                    displayedProjectInfo = true;
                }

                if (displayedProjectInfo)
                {
                    await _io.Output.WriteLineAsync();
                }
            }

            await foreach (var step in steps.ToAsyncEnumerable().WhereAwait(async s => await s.IsApplicableAsync(context, token)))
            {
                // Write indent (if any) and item number
                var indexStr = offset % 2 == 0
                    ? $"{count++}. "
                    : $"{(char)('a' + (count++ - 1))}. ";
                await _io.Output.WriteAsync($"{new string(' ', offset * 4)}{indexStr}");

                // Write the step title and make a note of whether the step is incomplete
                // (since that would mean future steps shouldn't show "[Next step]")
                WriteStepStatus(step, step == currentStep);
                await _io.Output.WriteLineAsync(step.Title);
                nextStepFound = nextStepFound || step.Status != UpgradeStepStatus.Complete;

                await ShowUpgradeStepsAsync(step.SubSteps, context, token, currentStep, offset + 1);
            }
        }

        private void WriteStepStatus(UpgradeStep step, bool isNextStep)
        {
            (ConsoleColor? color, var output) = step.Status switch
            {
                UpgradeStepStatus.Complete => (ConsoleColor.Green, "[Complete] "),
                UpgradeStepStatus.Failed => (ConsoleColor.Red, "[Failed] "),
                UpgradeStepStatus.Skipped => (ConsoleColor.DarkYellow, "[Skipped] "),
                UpgradeStepStatus.Incomplete when isNextStep => (ConsoleColor.Yellow, "[Next step] "),

                _ => default
            };

            if (color.HasValue && output is { Length: > 0 })
            {
                Console.ForegroundColor = color.Value;
                _io.Output.Write(output);
            }

            Console.ResetColor();
        }
    }
}
