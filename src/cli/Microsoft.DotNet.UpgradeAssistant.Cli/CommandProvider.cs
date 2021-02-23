﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Cli.Commands;
using Microsoft.DotNet.UpgradeAssistant.Upgrader.Commands;
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

        public IReadOnlyList<UpgradeCommand> GetCommands(UpgradeStep step)
        {
            if (step is null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            return new List<UpgradeCommand>(step.Commands)
            {
                new ApplyNextCommand(step),
                new SkipNextCommand(step),
                new SeeMoreDetailsCommand(step, ShowStepStatus),
                new ConfigureConsoleLoggingCommand(_userInput, _logSettings),
                _exit,
            };
        }

        private Task ShowStepStatus(UpgradeStep step)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            _io.Output.WriteLine(new string('-', step.Title.Length));
            _io.Output.WriteLine($"{step.Title}");
            _io.Output.WriteLine(new string('-', step.Title.Length));
            Console.ResetColor();
            _io.Output.WriteLine(ConsoleHelpers.WrapString(step.Description, Console.WindowWidth));
            WriteWithColor("  Status              ", ConsoleColor.DarkYellow);
            _io.Output.Write(": ");
            _io.Output.WriteLine($"{step.Status}");
            WriteWithColor("  Risk to break build ", ConsoleColor.DarkYellow);
            _io.Output.Write(": ");
            _io.Output.WriteLine($"{step.Risk}");
            WriteWithColor("  Details             ", ConsoleColor.DarkYellow);
            _io.Output.Write(": ");
            _io.Output.WriteLine(ConsoleHelpers.WrapString($"{step.StatusDetails}", Console.WindowWidth, "  Details             : ".Length));

            return Task.CompletedTask;
        }

        private void WriteWithColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            _io.Output.Write(msg);
            Console.ResetColor();
        }
    }
}
