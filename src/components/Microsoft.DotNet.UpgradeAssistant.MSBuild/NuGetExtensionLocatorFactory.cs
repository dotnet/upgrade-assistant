// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class NuGetExtensionLocatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public NuGetExtensionLocatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IExtensionLocator CreateLocator() =>
            new NuGetExtensionLocator(_serviceProvider.GetRequiredService<IVisualStudioFinder>(), _serviceProvider.GetRequiredService<ILogger<NuGetExtensionLocator>>());
    }
}
