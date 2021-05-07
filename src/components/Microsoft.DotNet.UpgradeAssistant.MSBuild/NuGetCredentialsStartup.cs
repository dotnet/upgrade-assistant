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
        private readonly string _inputPath;
        private readonly NuGetExtensionLocatorFactory _extensionLocatorFactory;
        private readonly IUserInput _userInput;
        private readonly ILogger<NuGetCredentialsStartup> _logger;

        public NuGetCredentialsStartup(UpgradeOptions upgradeOptions, NuGetExtensionLocatorFactory extensionLocatorFactory, IUserInput userInput, ILogger<NuGetCredentialsStartup> logger)
        {
            _inputPath = upgradeOptions?.ProjectPath ?? throw new ArgumentNullException(nameof(upgradeOptions));
            _extensionLocatorFactory = extensionLocatorFactory ?? throw new ArgumentNullException(nameof(extensionLocatorFactory));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            // TODO : Do we need to change plugin discovery root?
            // PluginDiscoveryUtility.InternalPluginDiscoveryRoot = new Lazy<string>(() => PluginDiscoveryUtility.GetInternalPluginRelativeToMSBuildDirectory(msbuildDirectory.Value.Path));

            if (HttpHandlerResourceV3.CredentialService == null)
            {
                _logger.LogDebug("Registering NuGet credential providers");
                PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders = true;
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
            // Setup NuGet variables here instead of in ctor because NuGet.Configuration can't be loaded until after all
            // startup actions have executed.
            var logger = new NuGetLogger(_logger);
            var settings = Settings.LoadDefaultSettings(Path.GetDirectoryName(_inputPath));
            var sourceProvider = new PackageSourceProvider(settings);

            var ret = new List<ICredentialProvider>
            {
                new SettingsCredentialProvider(sourceProvider, logger)
            };

            var securePluginProviders = await new SecurePluginCredentialProviderBuilder(PluginManager.Instance, false, new NuGetLogger(_logger)).BuildAllAsync().ConfigureAwait(false);
            ret.AddRange(securePluginProviders);

            if (PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders)
            {
                ret.Add(new DefaultNetworkCredentialsCredentialProvider());
            }

            var pluginProviders = new PluginCredentialProviderBuilder(_extensionLocatorFactory.CreateLocator(), GetDefaultSettings(), logger).BuildAll(NuGetVerbosity.Debug);
            ret.AddRange(pluginProviders);

            ret.Add(new ConsoleCredentialProvider());

            _logger.LogDebug("Using NuGet credential providers: {Providers}", string.Join(", ", ret.Select(p => p.Id)));

            return ret;
        }

        private ISettings GetDefaultSettings() => Settings.LoadDefaultSettings(Path.GetDirectoryName(_inputPath));
    }
}
