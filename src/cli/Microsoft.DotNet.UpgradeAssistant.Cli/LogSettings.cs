// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class LogSettings
    {
        public LogSettings(bool verbose)
        {
            Console = new LoggingLevelSwitch();
            File = new LoggingLevelSwitch();
            SetConsoleLevel(verbose ? LogLevel.Trace : LogLevel.Information);
            SetFileLevel(LogLevel.Debug);
        }

        public LoggingLevelSwitch Console { get; }

        public LoggingLevelSwitch File { get; }

        public void SetConsoleLevel(LogLevel level)
            => Console.MinimumLevel = GetLogLevel(level);

        public void SetFileLevel(LogLevel level)
            => File.MinimumLevel = GetLogLevel(level);

        private static LogEventLevel GetLogLevel(LogLevel newLogLevel)
            => newLogLevel switch
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
    }
}
