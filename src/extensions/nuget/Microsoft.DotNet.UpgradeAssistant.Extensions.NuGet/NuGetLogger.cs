// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using ILogger = NuGet.Common.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetLogger : ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public NuGetLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Log(ILogMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Temporary workaround for https://github.com/dotnet/upgrade-assistant/issues/1117
            // Suppressing all warnings to Debug or lower
            var logLevel = message.Level switch
            {
                LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Trace,
                _ => Microsoft.Extensions.Logging.LogLevel.Debug
            };

            _logger.Log(logLevel, "[NuGet] {NuGetMessage}", message.Message);
        }

        public void Log(LogLevel level, string data) => Log(new LogMessage(level, data));

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data) => Log(new LogMessage(LogLevel.Debug, data));

        public void LogError(string data) => Log(new LogMessage(LogLevel.Error, data));

        public void LogInformation(string data) => Log(new LogMessage(LogLevel.Information, data));

        public void LogInformationSummary(string data) => Log(new LogMessage(LogLevel.Information, data));

        public void LogMinimal(string data) => Log(new LogMessage(LogLevel.Minimal, data));

        public void LogVerbose(string data) => Log(new LogMessage(LogLevel.Verbose, data));

        public void LogWarning(string data) => Log(new LogMessage(LogLevel.Warning, data));
    }
}
