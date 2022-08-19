// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    public class ConfigUpdaterTest
    {
        public const string Input = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                                    <configuration>
                                        <system.serviceModel>
                                        <behaviors>
                                          <serviceBehaviors>
                                            <behavior name=""SampleBehavior"">
                                              <serviceMetadata httpGetEnabled = ""true"" />
                                              <serviceDebug includeExceptionDetailInFaults=""true""/>
                                            </behavior>
                                          </serviceBehaviors>
                                        </behaviors>
                                        <services>
                                            <service name=""SampleServices""  behaviorConfiguration=""SampleBehavior"">
                                            <endpoint address=""SampleService"" binding=""netTcpBinding"" contract=""SampleService.IService""/>
                                            <endpoint address=""SampleService2"" binding=""basicHttpsBinding"" contract=""SampleService.IService""/>
                                            <endpoint address = ""mex"" binding=""mexHttpBinding"" contract=""IMetadataExchange""/>
                                            <host>
                                                <baseAddresses>
                                                <add baseAddress = ""http://localhost:80/""/>
                                                <add baseAddress = ""https://localhost:443/sample/address""/>
                                                <add baseAddress= ""net.tcp://localhost:808/""/>
                                                </baseAddresses>
                                            </host>
                                            </service>
                                        </services>
                                        </system.serviceModel>
                                    </configuration>";

        public const string NotApplicableInput = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                                    <configuration>
                                        <system.serviceModel>
                                        <behaviors>
                                          <serviceBehaviors>
                                            <behavior name=""SampleBehavior"">
                                              <serviceMetadata httpGetEnabled = ""true"" />
                                            </behavior>
                                          </serviceBehaviors>
                                        </behaviors>
                                        <services>
                                        </services>
                                        </system.serviceModel>
                                    </configuration>";

        public const string ServiceDebug = @"<serviceDebug includeExceptionDetailInFaults=""true""/>";

        public const string Metadata = @"<serviceMetadata httpGetEnabled = ""true"" />";

        public const string MetadataHttps = @"<serviceMetadata httpsGetEnabled = ""true"" />";

        public const string MetadataBoth = @"<serviceMetadata httpGetEnabled = ""true"" httpsGetEnabled = ""true"" />";

        public const string MexEndpoint = @"<endpoint address = ""mex"" binding=""mexHttpBinding"" contract=""IMetadataExchange""/>";

        public const string Output = @"<configuration>
                                        <system.serviceModel>
                                        <!--The behavior element is not supported in configuration in CoreWCF. Some service behaviors, such as metadata, are configured in the source code.-->
                                        <!--<behaviors>
                                          <serviceBehaviors>
                                            <behavior name=""SampleBehavior"">
                                              <serviceMetadata httpGetEnabled = ""true"" />
                                              <serviceDebug includeExceptionDetailInFaults=""true""/>
                                            </behavior>
                                          </serviceBehaviors>
                                        </behaviors>-->
                                        <services>
                                            <service name=""SampleServices""  behaviorConfiguration=""SampleBehavior"">
                                            <endpoint address=""/SampleService"" binding=""netTcpBinding"" contract=""SampleService.IService""/>
                                            <endpoint address=""/sample/address/SampleService2"" binding=""basicHttpsBinding"" contract=""SampleService.IService""/>
                                            <!--The mex endpoint is removed because it's not support in CoreWCF. Instead, the metadata service is enabled in the source code.-->
                                            <!--The host element is not supported in configuration in CoreWCF. The port that endpoints listen on is instead configured in the source code.-->
                                            <!--<host>
                                                <baseAddresses>
                                                <add baseAddress = ""http://localhost:80/""/>
                                                <add baseAddress = ""https://localhost:443/sample/address""/>
                                                <add baseAddress= ""net.tcp://localhost:808/""/>
                                                </baseAddresses>
                                            </host>-->
                                            </service>
                                        </services>
                                        </system.serviceModel>
                                        </configuration>";

        private readonly NullLogger<ConfigUpdater> _logger = NullLogger<ConfigUpdater>.Instance;

        [Fact]
        public void GetUriTest()
        {
            Dictionary<string, Uri> expected = new Dictionary<string, Uri>();
            expected.Add(Uri.UriSchemeHttp, new Uri("http://localhost:80/"));
            expected.Add(Uri.UriSchemeHttps, new Uri("https://localhost:443/sample/address"));
            expected.Add(Uri.UriSchemeNetTcp, new Uri("net.tcp://localhost:808/"));

            var result = new ConfigUpdater(XDocument.Parse(Input), _logger).GetUri();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetBindingsTest()
        {
            HashSet<string> expected = new HashSet<string>();
            expected.Add("netTcpBinding");
            expected.Add("basicHttpsBinding");
            expected.Add("mexHttpBinding");
            var result = new ConfigUpdater(XDocument.Parse(Input), _logger).GetBindings();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(ServiceDebug, true)]
        public void SupportsServiceDebugTest(string replace, bool expected)
        {
            bool result = new ConfigUpdater(XDocument.Parse(Input.Replace(ServiceDebug, replace)), _logger).SupportsServiceDebug();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData(Metadata, 1)]
        [InlineData(MetadataHttps, 2)]
        [InlineData(MetadataBoth, 3)]
        public void SupportsMetadataBehaviorTest(string replace, int expected)
        {
            int result = new ConfigUpdater(XDocument.Parse(Input.Replace(Metadata, replace)), _logger).SupportsMetadataBehavior();
            var http = replace.Contains("httpGetEnabled");
            var https = replace.Contains("httpsGetEnabled");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(MexEndpoint, true)]
        public void IncludesMexEndpointTest(string replace, bool expected)
        {
            bool result = new ConfigUpdater(XDocument.Parse(Input.Replace(MexEndpoint, replace)), _logger).IncludesMexEndpoint();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void UpdateOldConfigTest()
        {
            var result = new ConfigUpdater(XDocument.Parse(Input), _logger).UpdateOldConfig();
            var expected = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                            <configuration >
                              <!-- system.serviceModel section is moved to a separate wcf.config file located at the same directory as this file.-->
                            </configuration> ";
            Assert.True(XNode.DeepEquals(XDocument.Parse(expected), result));
        }

        [Fact]
        public void GenerateNewConfigTest()
        {
            var result = new ConfigUpdater(XDocument.Parse(Input), _logger).GenerateNewConfig();
            Assert.Equal(Output.Replace(" ", string.Empty), result.ToString().Replace(" ", string.Empty));
        }

        [Fact]
        public void UpdateEndpointsTest()
        {
            var expected = @"endpoint address=""/SampleService""";
            var expected2 = @"endpoint address=""/sample/address/SampleService2""";
            var result = new ConfigUpdater(XDocument.Parse(Input), _logger).GenerateNewConfig().ToString();
            Assert.True(result.Contains(expected) && result.Contains(expected2));
        }

        [Fact]
        public void IsApplicableTest()
        {
            Assert.True(WCFUpdateChecker.IsConfigApplicable(XDocument.Parse(Input)));
            Assert.False(WCFUpdateChecker.IsConfigApplicable(XDocument.Parse(NotApplicableInput)));
        }
    }
}
