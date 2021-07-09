// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class UsedCommandTelemetry : IUpgradeStartup
    {
        private readonly ITelemetry _telemetry;
        private readonly ParseResult _result;

        public UsedCommandTelemetry(ITelemetry telemetry, ParseResult result)
        {
            _telemetry = telemetry;
            _result = result;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            _telemetry.TrackEvent("cli/command", new Dictionary<string, string>
            {
                { "Name", _result.CommandResult.Command.Name },
                { "Type", "command" },
            });

            foreach (var child in _result.CommandResult.Children)
            {
                var type = child switch
                {
                    ArgumentResult => "argument",
                    OptionResult => "option",
                    _ => "unknown"
                };

                var properties = new Dictionary<string, string>
                {
                    { "Name", child.Symbol.Name },
                    { "Type", type },
                };

                _telemetry.TrackEvent("cli/command", properties);
            }

            return Task.FromResult(true);
        }
    }
}
