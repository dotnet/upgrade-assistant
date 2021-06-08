// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace UpgradeStepSample
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
    public class SampleExtensionServiceProvider : IExtensionServiceProvider
    {
        private const string AuthorsPropertySectionName = "AuthorsProperty";

        /// <summary>
        /// Registers services needed for the AuthorsProperty sample extension in
        /// Upgrade Assistant's dependency injection container.
        /// </summary>
        /// <param name="services">A configuration object containing the service collection
        /// to register services in, the extension's configuration file, and a file provider
        /// for retrieving extension files.</param>
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Add the upgrade step to Upgrade Assistant's DI container so that it
            // will be used by the tool.
            services.Services.AddUpgradeStep<AuthorsPropertyUpgradeStep>();

            // This registers a type read from extension configuration. Using
            // AddExtensionOption (instead of registering an option using the
            // IExtensionServiceCollection's Configuration property) allows the
            // extension system to load the specified option from *all* extension,
            // not just the one registering it.
            //
            // Extensions can get the configured option from DI by requesting
            // IOptions<AuthorsPropertyOptions> (to get only the value of the option
            // specified by the most recently registered extension that includes the
            // option) or IOptions<ICollection<AuthorsPropertyOptions>> to get a collection
            // of all options of this type registered by any extensions.
            services.AddExtensionOption<AuthorsPropertyOptions>(AuthorsPropertySectionName);
        }
    }
}
