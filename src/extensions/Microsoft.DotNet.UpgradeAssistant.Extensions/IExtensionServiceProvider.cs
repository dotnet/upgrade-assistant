﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionServiceProvider
    {
        /// <summary>
        /// Implementers should use this method to register services for Upgrade Assistant to use.
        /// Often, this will be MigrationSteps. Any MigrationStep registered as a service here will be used
        /// by Upgrade Assistant. Besides MigrationSteps, this method might also be used to register services
        /// needed by migration steps (either those registered in this method ot those registered by other
        /// extensions). For example, registering services like Roslyn analyzers, IConfigUpdaters, or
        /// IPackageReferenceAnalyzers will cause migration steps that use those types to pick up the
        /// new implementations.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="serviceConfiguration">Configuration from the extension's ExtensionManifest.json file.</param>
        /// <returns>The service collection updated with services Upgrade Assistant should use.</returns>
        IServiceCollection AddServices(IServiceCollection services, IConfiguration serviceConfiguration);
    }
}
