// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

using ILogger = Microsoft.Build.Framework.ILogger;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class MSBuildExtensionsLogger : ILogger
    {
        private const string MSBuildLogPrefix = "[MSBuild] ";

        private readonly Microsoft.Extensions.Logging.ILogger _internalLogger;

        public LoggerVerbosity Verbosity { get; set; }

        public string? Parameters { get; set; }

        public MSBuildExtensionsLogger(Microsoft.Extensions.Logging.ILogger internalLogger, LoggerVerbosity verbosity)
        {
            _internalLogger = internalLogger ?? throw new ArgumentNullException(nameof(internalLogger));
            Verbosity = verbosity;
        }

        public void Initialize(IEventSource eventSource)
        {
            if (eventSource is null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            if (Verbosity > LoggerVerbosity.Normal)
            {
                eventSource.MessageRaised += LogInformation;
            }
            else
            {
                eventSource.BuildStarted += LogInformation;
                eventSource.BuildFinished += LogInformation;
            }

            eventSource.WarningRaised += LogWarning;
            eventSource.ErrorRaised += LogError;
            eventSource.CustomEventRaised += LogInformation;
        }

        private void LogInformation(object sender, BuildEventArgs eventArgs) => _internalLogger.LogDebug($"{MSBuildLogPrefix} INF: {eventArgs.Message}");

        private void LogWarning(object sender, BuildEventArgs eventArgs) => _internalLogger.LogDebug($"{MSBuildLogPrefix} WRN: {eventArgs.Message}");

        private void LogError(object sender, BuildEventArgs eventArgs) => _internalLogger.LogDebug($"{MSBuildLogPrefix} ERR: {eventArgs.Message}");

        public void Shutdown() { }
    }
}
