// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record ProcessInfo
    {
        public string Command { get; init; } = string.Empty;

        public string Arguments { get; init; } = string.Empty;

        public string? Name { get; init; }

        public int SuccessCode { get; init; }

        /// <summary>
        /// Gets a func that determines what log level should be used to log a message. The func takes a boolean indicating
        /// whether the message is from stderr and a string containing the text of the message.
        /// </summary>
        public Func<bool, string, LogLevel> GetMessageLogLevel { get; init; } = (isStdErr, _) => isStdErr ? LogLevel.Error : LogLevel.Information;

        public IEnumerable<KeyValuePair<string, string>> EnvironmentVariables { get; init; } = Enumerable.Empty<KeyValuePair<string, string>>();

        public Func<string, bool> IsErrorFilter { get; init; } = _ => false;

        public string DisplayName => Name ?? Path.GetFileNameWithoutExtension(Command);
    }
}
