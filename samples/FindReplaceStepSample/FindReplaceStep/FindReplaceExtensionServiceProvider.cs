// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FindReplaceStep
{
    /// <summary>
    /// Extension authors can implement the IExtensionServiceProvider interface to
    /// register services with Upgrade Assistant's dependency injection container.
    /// This could include registering additional upgrade steps. It might also include
    /// registering services needed by the steps registered or other migrations steps.
    /// For example, registering Roslyn analyzer/code fix providers, IConfigUpdaters,
    /// or IPackageReferenceAnalyzers will cause upgrade steps that use those types to
    /// pick the newly registered services up automatically and use them.
    /// </summary>
    public class FindReplaceExtensionServiceProvider : IExtensionServiceProvider
    {
        /// <summary>
        /// Registers services needed for the FindReplaceStep sample extension in
        /// Upgrade Assistant's dependency injection container.
        /// </summary>
        /// <param name="serviceConfiguration">A configuration object containing both the
        /// service collection to register services in and the extension's configuration file.</param>
        /// <returns>The service collection updated with services Upgrade Assistant should use.</returns>
        public IServiceCollection AddServices(ExtensionServiceConfiguration serviceConfiguration)
        {
            if (serviceConfiguration is null)
            {
                throw new ArgumentNullException(nameof(serviceConfiguration));
            }

            return serviceConfiguration.ServiceCollection.AddUpgradeStep<FindReplaceUpgradeStep>();
        }
    }
}
