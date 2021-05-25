// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class RazorHelperUpdaterTests
    {
        [Fact]
        public void CtorNegativeTests()
        {
            var mock = AutoMock.GetLoose();

            Assert.Throws<ArgumentNullException>("helperMatcher", () => new RazorHelperUpdater(null!, mock.Mock<ILogger<RazorHelperUpdater>>().Object));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorHelperUpdater(mock.Mock<IHelperMatcher>().Object, null!));
        }

        [Fact]
        public void PropertyTests()
        {
            var mock = AutoMock.GetLoose();
            var updater = mock.Create<RazorHelperUpdater>();

            Assert.Equal("Microsoft.DotNet.UpgradeAssistant.Steps.Razor.RazorHelperUpdater", updater.Id);
            Assert.Equal("Replace @helper syntax in Razor files", updater.Title);
            Assert.Equal("Update Razor documents to use local methods instead of @helper functions", updater.Description);
            Assert.Equal(BuildBreakRisk.Low, updater.Risk);
        }

        [Fact]
        public async Task IsApplicableNegativeTests()
        {
            var mock = AutoMock.GetLoose();
            var updater = mock.Create<RazorHelperUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.IsApplicableAsync(null!, Enumerable.Empty<RazorCodeDocument>().ToImmutableArray(), CancellationToken.None))
                .ConfigureAwait(true);
        }

        [Fact]
        public async Task ApplyNegativeTests()
        {
            var mock = AutoMock.GetLoose();
            var updater = mock.Create<RazorHelperUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.ApplyAsync(null!, Enumerable.Empty<RazorCodeDocument>().ToImmutableArray(), CancellationToken.None))
                .ConfigureAwait(true);
        }

        //[Theory]
        //public async Task IsApplicableTests(string[] inputTexts, string[] inputPaths, FileUpdaterResult expectedResult)
        //{
        //    var updater = new RazorHelperUpdater(new NullLogger<RazorHelperUpdater>());
        //    var inputs = GetRazorCodeDocuments(inputTexts, inputPaths); 
        //}
    }
}
