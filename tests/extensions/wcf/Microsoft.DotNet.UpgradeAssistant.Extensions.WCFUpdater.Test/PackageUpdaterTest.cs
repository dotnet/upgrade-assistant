// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Test
{
    public class PackageUpdaterTest
    {
        public const string Input = @"<?xml version=""1.0"" encoding=""utf - 8""?>
                            <Project Sdk = ""Microsoft.NET.Sdk.Web"">
                              <ItemGroup>
                                <PackageReference Include = ""Microsoft.CSharp"" Version = ""4.7.0"" />
                                <PackageReference Include = ""Serilog"" Version = ""2.8.0"" />
                                <PackageReference Include = ""System.ServiceModel.Duplex"" Version = ""4.8.1"" />
                              </ItemGroup>
                            </Project>";

        private readonly NullLogger _logger = NullLogger.Instance;

        [Fact]
        public void UpdateSDKTest()
        {
            string test = @"<?xml version=""1.0"" encoding=""utf - 8""?>
                            <Project Sdk = ""Microsoft.NET.Sdk""> 
                            </Project>";
            string expected = @"<?xml version=""1.0"" encoding=""utf - 8""?>
                            <Project Sdk = ""Microsoft.NET.Sdk.Web""> 
                            </Project>";
            var result = new PackageUpdater(XDocument.Parse(test), _logger).UpdateSDK();
            Assert.True(XNode.DeepEquals(XDocument.Parse(expected), result));
        }

        [Fact]
        public void UpdatePackagesTest()
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf - 8""?>
                            <Project Sdk = ""Microsoft.NET.Sdk.Web"">
                              <ItemGroup>
                                <PackageReference Include = ""Microsoft.CSharp"" Version = ""4.7.0"" />
                                <PackageReference Include = ""Serilog"" Version = ""2.8.0"" />
                                <PackageReference Include = ""CoreWCF.NetTcp"" Version = ""1.1.0"" />
                                <PackageReference Include = ""CoreWCF.Primitives"" Version = ""1.1.0"" />
                                <PackageReference Include = ""CoreWCF.ConfigurationManager"" Version = ""1.1.0"" />
                                <PackageReference Include = ""CoreWCF.Http"" Version = ""1.1.0"" />
                                <PackageReference Include = ""CoreWCF.WebHttp"" Version = ""1.1.0"" />
                              </ItemGroup>
                            </Project>";
            var result = new PackageUpdater(XDocument.Parse(Input), _logger).UpdatePackages();
            Assert.True(XNode.DeepEquals(XDocument.Parse(expected), result));
        }

        [Fact]
        public void IsApplicableTest()
        {
            string notApplicableInput = @"<?xml version=""1.0"" encoding=""utf - 8""?>
                            <Project Sdk = ""Microsoft.NET.Sdk.Web"">
                              <ItemGroup>
                                <PackageReference Include = ""Microsoft.CSharp"" Version = ""4.7.0"" />
                                <PackageReference Include = ""Serilog"" Version = ""2.8.0"" />
                              </ItemGroup>
                            </Project>";
            Assert.True(WCFUpdateChecker.IsProjFileApplicable(XDocument.Parse(Input)));
            Assert.False(WCFUpdateChecker.IsProjFileApplicable(XDocument.Parse(notApplicableInput)));
        }
    }
}
