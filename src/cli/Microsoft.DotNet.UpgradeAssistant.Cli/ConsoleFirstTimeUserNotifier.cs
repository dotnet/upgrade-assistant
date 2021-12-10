// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class ConsoleFirstTimeUserNotifier : IUpgradeStartup
    {
        private readonly IFirstTimeUseNoticeSentinel _sentinel;
        private readonly InputOutputStreams _io;

        public ConsoleFirstTimeUserNotifier(IFirstTimeUseNoticeSentinel sentinel, InputOutputStreams io)
        {
            _sentinel = sentinel ?? throw new ArgumentNullException(nameof(sentinel));
            _io = io ?? throw new ArgumentNullException(nameof(io));
        }

        public async Task<bool> StartupAsync(CancellationToken token)
        {
            if (!_sentinel.Exists())
            {
                await _io.Output.WriteLineAsync();
                await _io.Output.WriteLineAsync(_sentinel.Title);
                await _io.Output.WriteLineAsync(new string('-', 10));
                await _io.Output.WriteLineAsync(_sentinel.DisclosureText);
                await _io.Output.WriteLineAsync();

                _sentinel.CreateIfNotExists();
            }

            return true;
        }
    }
}
