// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class NonInteractiveUserInput : IUserInput
    {
        private readonly TimeSpan _waitPeriod;

        public NonInteractiveUserInput(IOptions<NonInteractiveOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _waitPeriod = options.Value.Wait;
        }

        public bool IsInteractive => false;

        public Task<string?> AskUserAsync(string prompt)
        {
            throw new NotImplementedException("User input cannot be selected in non-interactive mode");
        }

        public Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : UpgradeCommand
            => Task.FromResult(commands.First(c => c.IsEnabled));

        public async Task<bool> WaitToProceedAsync(CancellationToken token)
        {
            await Task.Delay(_waitPeriod, token);

            return true;
        }
    }
}
