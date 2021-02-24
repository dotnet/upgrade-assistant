// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands
{
    public class SeeMoreDetailsCommand : UpgradeCommand
    {
        private readonly UpgradeStep _step;
        private readonly Func<UpgradeStep, Task> _showDetails;

        public SeeMoreDetailsCommand(UpgradeStep step, Func<UpgradeStep, Task> showDetails)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _showDetails = showDetails ?? throw new ArgumentNullException(nameof(showDetails));
        }

        public override string CommandText => "See more step details";

        public override async Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
        {
            await _showDetails(_step).ConfigureAwait(false);
            return true;
        }
    }
}
