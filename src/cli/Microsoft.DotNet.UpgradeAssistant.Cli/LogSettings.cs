using System;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.UpgradeAssistant.Cli
{
    public class LogSettings
    {
        public LogSettings(bool verbose)
        {
            SelectedTargets = LogTargets.Console | LogTargets.File;
            LoggingLevelSwitch = new LoggingLevelSwitch();
            SetLogLevel(verbose ? LogLevel.Trace : LogLevel.Information);
        }

        public LogTargets SelectedTargets { get; set; }

        public LoggingLevelSwitch LoggingLevelSwitch { get; private set; }

        public bool IsFileEnabled => (SelectedTargets & LogTargets.File) == LogTargets.File;

        public bool IsConsoleEnabled => (SelectedTargets & LogTargets.Console) == LogTargets.Console;

        public void SetLogLevel(LogLevel newLogLevel)
        {
            LoggingLevelSwitch.MinimumLevel = newLogLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.None => LogEventLevel.Fatal,
                _ => throw new NotImplementedException()
            };

            if (newLogLevel == LogLevel.None)
            {
                SelectedTargets = LogTargets.None;
            }
        }
    }

    [Flags]
    public enum LogTargets
    {
        /// <summary>
        /// Logs should not be written.
        /// </summary>
        None,

        /// <summary>
        /// Logs should be written to the conosle.
        /// </summary>
        Console = 1,

        /// <summary>
        /// Logs should be written to a file on disk.
        /// </summary>
        File = 2
    }
}
