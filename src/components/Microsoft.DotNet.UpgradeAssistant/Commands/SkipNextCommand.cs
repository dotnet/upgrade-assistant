// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Commands
{
    public class SkipNextCommand : UpgradeCommand
    {
        private readonly UpgradeStep _step;

        public SkipNextCommand(UpgradeStep step)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
        }

        public override string Id => "Next";

        // todo - support localization
        public override string CommandText => $"Skip next step ({_step.Title})";

        public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
        {
            return _step.SkipAsync(token);
        }
    }
}
