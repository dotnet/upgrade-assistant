// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Cli.Commands;
using Microsoft.DotNet.UpgradeAssistant.Commands;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class CommandProvider
    {
        private readonly InputOutputStreams _io;
        private readonly LogSettings _logSettings;
        private readonly IUserInput _userInput;
        private readonly ExitCommand _exit;

        public CommandProvider(
            InputOutputStreams io,
            LogSettings logSettings,
            IUserInput userInput,
            IHostApplicationLifetime lifetime)
        {
            if (lifetime is null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            _io = io ?? throw new ArgumentNullException(nameof(io));
            _logSettings = logSettings;
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _exit = new ExitCommand(lifetime.StopApplication);
        }

        public IReadOnlyList<UpgradeCommand> GetCommands(UpgradeStep step, IUpgradeContext context)
        {
            if (step is null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var commands = new List<UpgradeCommand>
            {
                new ApplyNextCommand(step),
                new SkipNextCommand(step),
                new SeeMoreDetailsCommand(step, ShowStepStatus)
            };

            if (context.Projects.Count() > 1 && context.CurrentProject is not null)
            {
                commands.Add(new SelectProjectCommand());
            }

            commands.Add(new ConfigureConsoleLoggingCommand(_userInput, _logSettings));
            commands.Add(_exit);
            return commands;
        }

        private async Task ShowStepStatus(UpgradeStep step)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            await _io.Output.WriteLineAsync(new string('-', step.Title.Length));
            await _io.Output.WriteLineAsync($"{step.Title}");
            await _io.Output.WriteLineAsync(new string('-', step.Title.Length));
            Console.ResetColor();
            await _io.Output.WriteLineAsync(ConsoleHelpers.WrapString(step.Description, Console.WindowWidth));
            await WriteWithColorAsync("  Status              ", ConsoleColor.DarkYellow);
            await _io.Output.WriteAsync(": ");
            await _io.Output.WriteLineAsync($"{step.Status}");
            await WriteWithColorAsync("  Risk to break build ", ConsoleColor.DarkYellow);
            await _io.Output.WriteAsync(": ");
            await _io.Output.WriteLineAsync($"{step.Risk}");
            await WriteWithColorAsync("  Details             ", ConsoleColor.DarkYellow);
            await _io.Output.WriteAsync(": ");
            await _io.Output.WriteLineAsync(ConsoleHelpers.WrapString($"{step.StatusDetails}", Console.WindowWidth, "  Details             : ".Length));
        }

        private async ValueTask WriteWithColorAsync(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            await _io.Output.WriteAsync(msg);
            Console.ResetColor();
        }
    }
}
