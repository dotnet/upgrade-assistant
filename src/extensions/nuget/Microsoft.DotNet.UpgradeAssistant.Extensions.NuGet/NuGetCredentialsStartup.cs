// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Credentials;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetCredentialsStartup : IUpgradeStartup
    {
        private readonly IUserInput _userInput;
        private readonly ILogger<NuGetCredentialsStartup> _logger;

        public NuGetCredentialsStartup(IUserInput userInput, ILogger<NuGetCredentialsStartup> logger)
        {
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            DefaultCredentialServiceUtility.SetupDefaultCredentialService(new NuGetLogger(_logger), !_userInput.IsInteractive);
            return Task.FromResult(true);
        }
    }
}
