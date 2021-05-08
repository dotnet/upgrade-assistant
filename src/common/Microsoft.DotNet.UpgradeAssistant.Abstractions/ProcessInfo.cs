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

        public Func<string, LogLevel> GetMessageLogLevel { get; init; } = msg => msg.Contains("Error") ? LogLevel.Error : LogLevel.Information;

        public IEnumerable<KeyValuePair<string, string>> EnvironmentVariables { get; init; } = Enumerable.Empty<KeyValuePair<string, string>>();

        public Func<string, bool> IsErrorFilter { get; init; } = _ => false;

        public string DisplayName => Name ?? Path.GetFileNameWithoutExtension(Command);
    }
}
