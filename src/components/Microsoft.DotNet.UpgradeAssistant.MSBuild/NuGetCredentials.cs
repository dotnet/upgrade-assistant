// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGet.Protocol.Plugins;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public static class NuGetCredentials
    {
        public static void ConfigureCredentialService(bool interactive, ILogger logger)
        {
            if (HttpHandlerResourceV3.CredentialService == null)
            {
                HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(
                    () => new CredentialService(
                        providers: new AsyncLazy<IEnumerable<ICredentialProvider>>(async () => await GetCredentialProvidersAsync(logger).ConfigureAwait(false)),
                        nonInteractive: !interactive,
                        handlesDefaultCredentials: PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders));
            }
        }

        public static async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync(ILogger logger)
        {
            var pluginProviderBuilder = new SecurePluginCredentialProviderBuilder(PluginManager.Instance, false, logger);
            var pluginProviders = (await pluginProviderBuilder.BuildAllAsync().ConfigureAwait(false)).ToList();

            if (PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders)
            {
                pluginProviders.Add(new DefaultNetworkCredentialsCredentialProvider());
            }

            return pluginProviders;
        }
    }
}
