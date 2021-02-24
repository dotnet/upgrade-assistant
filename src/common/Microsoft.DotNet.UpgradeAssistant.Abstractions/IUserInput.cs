// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUserInput
    {
        Task<string?> AskUserAsync(string prompt);

        Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : UpgradeCommand;

        Task<bool> WaitToProceedAsync(CancellationToken token);
    }
}
