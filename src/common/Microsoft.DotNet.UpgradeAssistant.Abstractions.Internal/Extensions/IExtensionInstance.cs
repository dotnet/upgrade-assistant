// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public interface IExtensionInstance
    {
        IConfiguration Configuration { get; }

        IFileProvider FileProvider { get; }

        string Location { get; }

        Version? MinUpgradeAssistantVersion { get; }

        string Name { get; }

        string Description { get; }

        IReadOnlyCollection<string> Authors { get; }

        Version? Version { get; }

        void AddServices(IServiceCollection services);

        T? GetOptions<T>(string sectionName);
    }
}
