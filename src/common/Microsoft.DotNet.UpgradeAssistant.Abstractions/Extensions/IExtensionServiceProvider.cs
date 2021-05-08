// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionServiceProvider
    {
        /// <summary>
        /// Implementers should use this method to register services for Upgrade Assistant to use.
        /// Often, this will be UpgradeSteps. Any UpgradeStep registered as a service here will be used
        /// by Upgrade Assistant. Besides UpgradeSteps, this method might also be used to register services
        /// needed by upgrade steps (either those registered in this method ot those registered by other
        /// extensions). For example, registering services like Roslyn analyzers, IConfigUpdaters, or
        /// IPackageReferenceAnalyzers will cause upgrade steps that use those types to pick up the
        /// new implementations.
        /// </summary>
        /// <param name="services">The extension's service configuration, including the service collection
        /// to register services into and configuration from the extension's ExtensionManifest.json file.</param>
        void AddServices(IExtensionServiceCollection services);
    }
}
