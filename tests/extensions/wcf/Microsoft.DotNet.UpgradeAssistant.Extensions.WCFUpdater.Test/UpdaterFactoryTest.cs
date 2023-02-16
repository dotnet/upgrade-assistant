// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    public class UpdaterFactoryTest
    {
        private readonly NullLogger _logger = NullLogger.Instance;
        private readonly NullLogger<ConfigUpdater> _configLogger = NullLogger<ConfigUpdater>.Instance;

        [Theory]
        [InlineData("TestInputFiles\\DebugConfig.txt", "TestExpectedFiles\\DebugTemplateCode.txt")]
        [InlineData("TestInputFiles\\CredentialsConfig.txt", "TestExpectedFiles\\CredentialsTemplateCode.txt")]
        [InlineData("TestInputFiles\\CredentialsHttpsConfig.txt", "TestExpectedFiles\\CredentialsHttpsTemplateCode.txt")]
        [InlineData("TestInputFiles\\SampleConfig.txt", "TestExpectedFiles\\SimpleTemplateCode.txt")]
        [InlineData("TestInputFiles\\MultiServicesConfig.txt", "TestExpectedFiles\\MultiServicesTemplateCode.txt")]
        public void UpdateFactoryTemplateTest(string input, string expected)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            input = input.Replace('\\', Path.DirectorySeparatorChar);
            expected = expected.Replace('\\', Path.DirectorySeparatorChar);

            var context = new ConfigContext(new ConfigUpdater(XDocument.Load(input), _configLogger));
            var actual = UpdaterFactory.UpdateTemplateCode(context, _logger);
            Assert.Equal(File.ReadAllText(expected), actual);
        }
    }
}
