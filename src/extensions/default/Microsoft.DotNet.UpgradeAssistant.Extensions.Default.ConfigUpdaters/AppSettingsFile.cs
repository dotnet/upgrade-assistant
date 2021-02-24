// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters
{
    public class AppSettingsFile
    {
        public AppSettingsFile(string path)
        {
            FilePath = path;
            Configuration = new ConfigurationBuilder()
                .AddJsonFile(path)
                .Build();
        }

        public string FilePath { get; }

        public IConfiguration Configuration { get; }
    }
}
