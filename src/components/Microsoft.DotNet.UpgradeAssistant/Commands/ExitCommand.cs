// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Commands
{
    public class ExitCommand : UpgradeCommand
    {
        private readonly Action _stopTheProgram;

        public ExitCommand(Action stopTheProgram)
        {
            _stopTheProgram = stopTheProgram ?? throw new ArgumentNullException(nameof(stopTheProgram));
        }

        public override string Id => "Exit";

        // todo - support localization
        public override string CommandText => "Exit";

        public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
        {
            _stopTheProgram();
            return Task.FromResult(true);
        }
    }
}
