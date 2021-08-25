// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionInstanceFactory
    {
        private readonly ILogger<ExtensionInstance> _logger;
        private readonly string _instanceKey;

        public ExtensionInstanceFactory(ILogger<ExtensionInstance> logger)
        {
            _logger = logger;
            _instanceKey = Guid.NewGuid().ToString("N");
        }

        public ExtensionInstance Create(IFileProvider fileProvider, string location, IConfiguration configuration)
            => new ExtensionInstance(_instanceKey, fileProvider, location, configuration, _logger);

        public ExtensionInstance Create(IFileProvider fileProvider, string location)
            => Create(fileProvider, location, CreateConfiguration(fileProvider));

        private static IConfiguration CreateConfiguration(IFileProvider fileProvider)
        {
            try
            {
                return new ConfigurationBuilder()
                    .AddJsonFile(fileProvider, ExtensionInstance.ManifestFileName, optional: false, reloadOnChange: false)
                    .Build();
            }
            catch (FileNotFoundException e)
            {
                throw new UpgradeException("Could not find manifest file", e);
            }
        }
    }
}
