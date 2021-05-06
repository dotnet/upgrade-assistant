// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ISettings _nugetSettings;
        private readonly IUserInput _userInput;
        private readonly ILogger<NuGetCredentialsStartup> _logger;

        public NuGetCredentialsStartup(UpgradeOptions upgradeOptions, IUserInput userInput, ILogger<NuGetCredentialsStartup> logger)
        {
            if (upgradeOptions is null)
            {
                throw new ArgumentNullException(nameof(upgradeOptions));
            }

            _nugetSettings = Settings.LoadDefaultSettings(Path.GetDirectoryName(upgradeOptions.ProjectPath));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            if (HttpHandlerResourceV3.CredentialService == null)
            {
                _logger.LogDebug("Registering NuGet credential providers");
                var credentialService = new CredentialService(
                        providers: new AsyncLazy<IEnumerable<ICredentialProvider>>(async () => await GetCredentialProvidersAsync().ConfigureAwait(false)),
                        nonInteractive: !_userInput.IsInteractive,
                        handlesDefaultCredentials: PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders);

                HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(() => credentialService);
            }

            return Task.FromResult(true);
        }

        private async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync()
        {
            // TODO : Expand to be more like https://github.com/NuGet/NuGet.Client/blob/17c4f841ff61d27fe6b57cf45ceef16037062635/src/NuGet.Clients/NuGet.CommandLine/Commands/Command.cs#L209
            var logger = new NuGetLogger(_logger);
            var ret = new List<ICredentialProvider>();

            var pluginProviders = new PluginCredentialProviderBuilder(null! /* TODO */, _nugetSettings, logger).BuildAll(NuGetVerbosity.Normal);
            ret.AddRange(pluginProviders);

            var securePluginProviders = new SecurePluginCredentialProviderBuilder(PluginManager.Instance, true, new NuGetLogger(_logger));
            ret.AddRange(await securePluginProviders.BuildAllAsync().ConfigureAwait(false));

            if (ret.Any() && PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders)
            {
                ret.Add(new DefaultNetworkCredentialsCredentialProvider());
            }

            ret.Add(new ConsoleCredentialProvider(logger));

            _logger.LogDebug("Using NuGet credential providers: {Providers}", string.Join(", ", pluginProviders.Select(p => p.Id)));

            return pluginProviders;
        }
    }
}
