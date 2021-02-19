// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters.Tests
{
    public class UnsupportedSectionConfigUpdaterTests
    {
        [Fact]
        public async Task NoConfig()
        {
            var context = Substitute.For<IMigrationContext>();
            var logger = Substitute.For<ILogger<UnsupportedSectionConfigUpdater>>();
            var updater = new UnsupportedSectionConfigUpdater(logger);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create<ConfigFile>(), default);
            Assert.False(isAvailable);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create<ConfigFile>(), default);
            Assert.False(isApplied);
        }

        [Fact]
        public async Task SingleConfigNoConfigSection()
        {
            var context = Substitute.For<IMigrationContext>();
            var logger = Substitute.For<ILogger<UnsupportedSectionConfigUpdater>>();
            var updater = new UnsupportedSectionConfigUpdater(logger);
            var config = @"<?xml version=""1.0""?>" +
                @"<NotConfiguration />";
            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default);
            Assert.False(isAvailable);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default);
            Assert.False(isApplied);
        }

        [Fact]
        public async Task SingleConfigHasConfigSection()
        {
            var context = Substitute.For<IMigrationContext>();
            var logger = Substitute.For<ILogger<UnsupportedSectionConfigUpdater>>();
            var updater = new UnsupportedSectionConfigUpdater(logger);
            var config = @"<?xml version=""1.0""?>" +
                @"<configuration />";
            var configFile = CreateFile(config);

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default);
            Assert.False(isAvailable);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default);
            Assert.False(isApplied);
        }

        [Fact]
        public async Task SingleConfigHasSystemDiagnosticsSection()
        {
            var context = Substitute.For<IMigrationContext>();
            var logger = Substitute.For<ILogger<UnsupportedSectionConfigUpdater>>();
            var updater = new UnsupportedSectionConfigUpdater(logger);
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

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default);
            Assert.True(isAvailable);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default);
            Assert.True(isApplied);

            AssertConfigEquals(configFile, after);
        }

        [Fact]
        public async Task SingleConfigHasSystemDiagnosticsSectionWithChildren()
        {
            var context = Substitute.For<IMigrationContext>();
            var logger = Substitute.For<ILogger<UnsupportedSectionConfigUpdater>>();
            var updater = new UnsupportedSectionConfigUpdater(logger);
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

            var isAvailable = await updater.IsApplicableAsync(context, ImmutableArray.Create(configFile), default);
            Assert.True(isAvailable);

            var isApplied = await updater.ApplyAsync(context, ImmutableArray.Create(configFile), default);
            Assert.True(isApplied);

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
            var actual = File.ReadAllText(actualConfig.Path).Replace("\r", string.Empty);
            expected = expected.Replace("\r", string.Empty);

            Assert.Equal(actual, expected);
        }
    }
}
