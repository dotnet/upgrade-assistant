// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Tests
{
    public class ExtensionProviderTests
    {
        [Fact]
        public void GivenCustomExtensionShouldBeRegisteredAfterDefaultExtension()
        {
            var defaultPath = "default";
            var customPath = "custom";
            var options = new ExtensionOptions();
            options.DefaultExtensions.Add(defaultPath);
            options.ExtensionPaths.Add(customPath);
            var loader = new Mock<IExtensionLoader>();
            using (var defaultInstance = new ExtensionInstance(defaultPath, Mock.Of<IFileProvider>(), defaultPath, MockConfiguration(), NullLogger<ExtensionInstance>.Instance))
            {
                using (var customInstance = new ExtensionInstance(customPath, Mock.Of<IFileProvider>(), customPath, MockConfiguration(), NullLogger<ExtensionInstance>.Instance))
                {
                    loader.Setup(l => l.LoadExtension(It.Is<string>(s => s.EndsWith(defaultPath)))).Returns(defaultInstance);
                    loader.Setup(l => l.LoadExtension(It.Is<string>(s => s.EndsWith(customPath)))).Returns(customInstance);
                    using (var subject = new ExtensionProvider(new[] { loader.Object }, MockConfigurationLoader(), new ExtensionInstanceFactory(NullLogger<ExtensionInstance>.Instance), Mock.Of<IExtensionLocator>(), Options.Create(options), NullLogger<ExtensionProvider>.Instance))
                    {
                        Assert.Equal((subject as IExtensionProvider).Instances.First().Location, defaultPath);
                    }
                }
            }
        }

        private static IConfiguration MockConfiguration()
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(Mock.Of<IConfigurationSection>());
            return configuration.Object;
        }

        private static IUpgradeAssistantConfigurationLoader MockConfigurationLoader()
        {
            var configurationLoader = new Mock<IUpgradeAssistantConfigurationLoader>();
            configurationLoader.Setup(cl => cl.Load()).Returns(new UpgradeAssistantConfiguration());
            return configurationLoader.Object;
        }
    }
}
