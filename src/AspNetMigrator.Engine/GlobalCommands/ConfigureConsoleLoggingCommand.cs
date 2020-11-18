using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Logging;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class ConfigureConsoleLoggingCommand : MigrationCommand
    {
        private readonly LogSettings _logSettings;
        private readonly ILogger _logger;

        public ConfigureConsoleLoggingCommand(LogSettings logSettings, ILogger<ConfigureConsoleLoggingCommand> logger)
        {
            _logSettings = logSettings ?? throw new ArgumentNullException(nameof(logSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    _logSettings.SelectedTarget = LogTarget.None;
                }
                else
                {
                    Console.WriteLine("Choose where to send log messages:");
                    Console.WriteLine("0) Console");
                    Console.WriteLine("1) File");
                    Console.WriteLine("2) Both");

                    var selectedTarget = Console.ReadLine();
                    if (Enum.TryParse<LogTarget>(selectedTarget, out var newTarget))
                    {
                        _logSettings.SelectedTarget = newTarget;
                    }
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
