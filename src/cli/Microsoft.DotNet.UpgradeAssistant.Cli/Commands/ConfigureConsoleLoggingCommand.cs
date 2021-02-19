// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands
{
    public class ConfigureConsoleLoggingCommand : MigrationCommand
    {
        private readonly IUserInput _userInput;
        private readonly LogSettings _logSettings;

        public ConfigureConsoleLoggingCommand(IUserInput userInput, LogSettings logSettings)
        {
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logSettings = logSettings ?? throw new ArgumentNullException(nameof(logSettings));
        }

        public override string CommandText => "Configure logging";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            var result = await _userInput.ChooseAsync("Choose your log level:", CreateFromEnum<LogLevel>(), token);
            var newLogLevel = result.Value;

            // if the choice cannot be parsed then we will not change the setting
            _logSettings.SetLogLevel(newLogLevel);

            if (newLogLevel == LogLevel.None)
            {
                _logSettings.SelectedTargets = LogTargets.None;
            }
            else
            {
                var target = LogTargets.None;

                var commands = new[]
                {
                    Create("Console", () => target = LogTargets.Console),
                    Create("File", () => target = LogTargets.File),
                    Create("Both", () => target = LogTargets.Console | LogTargets.File),
                };

                await _userInput.ChooseAsync("Choose where to send log messages:", commands, token);

                _logSettings.SelectedTargets = target;
            }

            return true;
        }
    }
}
