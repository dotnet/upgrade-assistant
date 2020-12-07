using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetMigrator.Commands;
using AspNetMigrator.ConsoleApp.Commands;
using Microsoft.Extensions.Hosting;

namespace AspNetMigrator.ConsoleApp
{
    public class CommandProvider
    {
        private readonly InputOutputStreams _io;
        private readonly LogSettings _logSettings;
        private readonly ExitCommand _exit;

        public CommandProvider(InputOutputStreams io, LogSettings logSettings, IHostApplicationLifetime lifetime)
        {
            if (lifetime is null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            _io = io ?? throw new ArgumentNullException(nameof(io));
            _logSettings = logSettings;

            _exit = new ExitCommand(lifetime.StopApplication);
        }

        public IReadOnlyList<MigrationCommand> GetCommands(MigrationStep step)
        {
            if (step is null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            return new List<MigrationCommand>(step.Commands)
            {
                new ApplyNextCommand(step),
                new SkipNextCommand(step),
                new SeeMoreDetailsCommand(step, ShowStepStatus),
                new ConfigureConsoleLoggingCommand(_io, _logSettings),
                _exit,
            };
        }

        private Task ShowStepStatus(UserMessage stepStatus)
        {
            _io.Output.WriteLine("Current step details");
            return ConsoleHelpers.SendMessageToUserAsync(_io.Output, stepStatus);
        }
    }
}
