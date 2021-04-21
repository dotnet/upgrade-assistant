// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGet.Protocol.Plugins;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
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
            if (HttpHandlerResourceV3.CredentialService == null)
            {
                _logger.LogDebug("Registering NuGet credential providers");
                HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(
                    () => new CredentialService(
                        providers: new AsyncLazy<IEnumerable<ICredentialProvider>>(async () => await GetCredentialProvidersAsync().ConfigureAwait(false)),
                        nonInteractive: !_userInput.IsInteractive,
                        handlesDefaultCredentials: PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders));
            }

            return Task.FromResult(true);
        }

        private async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync()
        {
            var pluginProviderBuilder = new SecurePluginCredentialProviderBuilder(PluginManager.Instance, false, new NuGetLogger(_logger));
            var pluginProviders = (await pluginProviderBuilder.BuildAllAsync().ConfigureAwait(false)).ToList();

            if (PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders)
            {
                pluginProviders.Add(new DefaultNetworkCredentialsCredentialProvider());
            }

            _logger.LogDebug("Using NuGet credential providers: {Providers}", string.Join(", ", pluginProviders.Select(p => p.Id)));

            return pluginProviders;
        }
    }
}
