using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConsoleApp.Commands
{
    public class ConfigureConsoleLoggingCommand : MigrationCommand
    {
        private readonly LogSettings _logSettings;

        public ConfigureConsoleLoggingCommand(LogSettings logSettings)
        {
            _logSettings = logSettings ?? throw new ArgumentNullException(nameof(logSettings));
        }

        // todo - support localization
        public override string CommandText => "Configure logging";

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            Console.WriteLine("Choose your log level:");
            Console.WriteLine("0) Trace");
            Console.WriteLine("1) Debug");
            Console.WriteLine("2) Info");
            Console.WriteLine("3) Warning");
            Console.WriteLine("4) Error");
            Console.WriteLine("5) Critical");
            Console.WriteLine("6) None");
            Console.WriteLine();

            var selectedLogLevel = Console.ReadLine();

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
                    Console.WriteLine("Choose where to send log messages:");
                    Console.WriteLine("1) Console");
                    Console.WriteLine("2) File");
                    Console.WriteLine("3) Both");

                    var selectedTarget = Console.ReadLine();

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
