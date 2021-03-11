// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Commands
{
    public class SelectProjectCommand : UpgradeCommand
    {
        public override string CommandText => "Select different project";

        public override Task<bool> ExecuteAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.SetCurrentProject(null);

            return Task.FromResult(true);
        }
    }
}
