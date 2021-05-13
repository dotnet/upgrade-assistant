// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionServiceProvider
    {
        /// <summary>
        /// Method to register services for Upgrade Assistant to use. This may include the following:
        ///
        ///   - <see cref="UpgradeStep"/>
        ///   - <see cref="IDependencyAnalyzer"/>
        ///   - <see cref="IUpdater{T}"/>
        ///
        /// This can also be used to register options for extension specific or options that can be
        /// collected from all extensions.
        /// </summary>
        /// <param name="services">The extension's service configuration.</param>
        void AddServices(IExtensionServiceCollection services);
    }
}
