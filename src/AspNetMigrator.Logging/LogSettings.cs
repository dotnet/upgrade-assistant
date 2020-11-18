using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace AspNetMigrator.Logging
{
    public class LogSettings
    {
        public LogSettings()
        {
            SelectedTarget = LogTarget.Both;
            LoggingLevelSwitch = new LoggingLevelSwitch();
        }

        public LogTarget SelectedTarget { get; set; }

        public LoggingLevelSwitch LoggingLevelSwitch { get; private set; }

        public bool IsFileEnabled
        {
            get
            {
                return SelectedTarget == LogTarget.Both || SelectedTarget == LogTarget.File;
            }
        }

        public bool IsConsoleEnabled
        {
            get
            {
                return SelectedTarget == LogTarget.Both || SelectedTarget == LogTarget.Console;
            }
        }

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
                LogLevel.None => LogEventLevel.Fatal
            };

            if (newLogLevel == LogLevel.None)
            {
                SelectedTarget = LogTarget.None;
            }
        }
    }

    public enum LogTarget
    {
        Console = 0,
        File = 1,
        Both = 2,
        None = 3
    }
}
