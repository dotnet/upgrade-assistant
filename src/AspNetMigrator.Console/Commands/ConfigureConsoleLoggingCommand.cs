using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp.Commands
{
    public class ConfigureConsoleLoggingCommand : MigrationCommand
    {
        private readonly InputOutputStreams _io;
        private readonly LogSettings _logSettings;

        public ConfigureConsoleLoggingCommand(InputOutputStreams io, LogSettings logSettings)
        {
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _logSettings = logSettings ?? throw new ArgumentNullException(nameof(logSettings));
        }

        // todo - support localization
        public override string CommandText => "Configure logging";

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            _io.Output.WriteLine("Choose your log level:");
            _io.Output.WriteLine("0) Trace");
            _io.Output.WriteLine("1) Debug");
            _io.Output.WriteLine("2) Info");
            _io.Output.WriteLine("3) Warning");
            _io.Output.WriteLine("4) Error");
            _io.Output.WriteLine("5) Critical");
            _io.Output.WriteLine("6) None");
            _io.Output.WriteLine();

            var selectedLogLevel = _io.Input.ReadLine();

            if (Enum.TryParse<LogLevel>(selectedLogLevel, out var newLogLevel))
            {
                // if the choice cannot be parsed then we will not change the setting
                _logSettings.SetLogLevel(newLogLevel);

                if (newLogLevel == LogLevel.None)
                {
                    _logSettings.SelectedTargets = LogTargets.None;
                }
                else
                {
                    _io.Output.WriteLine("Choose where to send log messages:");
                    _io.Output.WriteLine("1) Console");
                    _io.Output.WriteLine("2) File");
                    _io.Output.WriteLine("3) Both");

                    var selectedTarget = _io.Input.ReadLine();

                    if (int.TryParse(selectedTarget, out var targetInt))
                    {
                        _logSettings.SelectedTargets = targetInt switch
                        {
                            1 => LogTargets.Console,
                            2 => LogTargets.File,
                            3 => LogTargets.Console | LogTargets.File,
                            _ => LogTargets.None
                        };
                    }
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
