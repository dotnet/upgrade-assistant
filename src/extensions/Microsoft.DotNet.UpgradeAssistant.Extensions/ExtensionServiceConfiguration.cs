// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    // This is a false positive
#pragma warning disable CA2227 // Collection properties should be read only
    public record ExtensionServiceConfiguration(IServiceCollection ServiceCollection, IConfiguration ExtensionConfiguration);
#pragma warning restore CA2227 // Collection properties should be read only
}
