// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands
{
    public class ConfigureConsoleLoggingCommand : UpgradeCommand
    {
        private readonly IUserInput _userInput;
        private readonly LogSettings _logSettings;

        public ConfigureConsoleLoggingCommand(IUserInput userInput, LogSettings logSettings)
        {
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logSettings = logSettings ?? throw new ArgumentNullException(nameof(logSettings));
        }

        public override string Id => "Logging";

        public override string CommandText => "Configure logging";

        private enum LogTarget
        {
            Console,
            File
        }

        public override async Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
        {
            var target = await _userInput.ChooseAsync("Choose log target:", CreateFromEnum<LogTarget>(), token);
            var result = await _userInput.ChooseAsync("Choose your log level:", CreateFromEnum<LogLevel>(), token);

            switch (target.Value)
            {
                case LogTarget.Console:
                    _logSettings.SetConsoleLevel(result.Value);
                    return true;
                case LogTarget.File:
                    _logSettings.SetFileLevel(result.Value);
                    return true;
                default:
                    return false;
            }
        }
    }
}
