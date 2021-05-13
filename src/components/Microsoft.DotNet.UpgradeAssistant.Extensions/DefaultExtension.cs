// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class DefaultExtension : IExtension
    {
        private readonly IConfiguration _configuration;

        public virtual string Name => "Default extension";

        public IFileProvider Files { get; }

        public DefaultExtension(IConfiguration configuration)
            : this(configuration, AppContext.BaseDirectory)
        {
        }

        public DefaultExtension(IConfiguration configuration, string baseDirectory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Files = new PhysicalFileProvider(baseDirectory);
        }

        public T? GetOptions<T>(string sectionName) => _configuration.GetSection(sectionName).Get<T>();
    }
}
