// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters.Tests
{
    public class UnsupportedSectionConfigUpdaterTests
    {
        [Fact]
        public async Task NoConfig()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var updater = mock.Create<UnsupportedSectionConfigUpdater>();

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create<ConfigFile>(), default).ConfigureAwait(false);
            Assert.False(isAvailable.Result);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create<ConfigFile>(), default).ConfigureAwait(false);
            Assert.False(isApplied.Result);
        }

        [Fact]
        public async Task SingleConfigNoConfigSection()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var updater = mock.Create<UnsupportedSectionConfigUpdater>();

            var config = @"<?xml version=""1.0""?>" +
                @"<NotConfiguration />";
            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.False(isAvailable.Result);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.False(isApplied.Result);
        }

        [Fact]
        public async Task SingleConfigHasConfigSection()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var updater = mock.Create<UnsupportedSectionConfigUpdater>();

            var config = @"<?xml version=""1.0""?>" +
                @"<configuration />";
            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.False(isAvailable.Result);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.False(isApplied.Result);
        }

        [Fact]
        public async Task SingleConfigHasSystemDiagnosticsSection()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var updater = mock.Create<UnsupportedSectionConfigUpdater>();

            var config = @"<?xml version=""1.0""?>
<configuration>
    <system.diagnostics/>
</configuration>";
            var after = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <!-- system.diagnostics section is not supported on .NET 5 (see https://github.com/dotnet/runtime/issues/23937)-->
    <!--<system.diagnostics />-->
</configuration>";

            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.True(isAvailable.Result);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.True(isApplied.Result);

            AssertConfigEquals(configFile, after);
        }

        [Fact]
        public async Task SingleConfigHasSystemDiagnosticsSectionWithChildren()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var updater = mock.Create<UnsupportedSectionConfigUpdater>();

            var config = @"<?xml version=""1.0""?>
<configuration>
    <system.diagnostics>
        <child />
    </system.diagnostics>
</configuration>";
            var after = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <!-- system.diagnostics section is not supported on .NET 5 (see https://github.com/dotnet/runtime/issues/23937)-->
    <!--<system.diagnostics>
  <child />
</system.diagnostics>-->
</configuration>";

            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.True(isAvailable.Result);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default).ConfigureAwait(false);
            Assert.True(isApplied.Result);

            AssertConfigEquals(configFile, after);
        }

        private static ConfigFile CreateFile(string contents)
        {
            var path = Path.GetTempFileName() + ".app.config";

            File.WriteAllText(path, contents);

            return new ConfigFile(path);
        }

        private static void AssertConfigEquals(ConfigFile actualConfig, string expected)
        {
            var actual = File.ReadAllText(actualConfig.Path).Replace("\r", string.Empty, StringComparison.OrdinalIgnoreCase);
            expected = expected.Replace("\r", string.Empty, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(actual, expected);
        }
    }
}
