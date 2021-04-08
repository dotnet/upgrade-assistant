// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class RazorUpdaterStepTests
    {
        [Fact]
        public void CtorTests()
        {
            // Arrange
            using var mock = GetMock(1, 2);

            // Act
            var step = mock.Create<RazorUpdaterStep>();

            // Assert
            Assert.Collection(
                step.DependencyOf,
                d => Assert.Equal(WellKnownStepIds.NextProjectStepId, d));
            Assert.Collection(
                step.DependsOn.OrderBy(x => x),
                d => Assert.Equal(WellKnownStepIds.BackupStepId, d),
                d => Assert.Equal(WellKnownStepIds.SetTFMStepId, d),
                d => Assert.Equal(WellKnownStepIds.TemplateInserterStepId, d));
            Assert.Equal("Update Razor files using registered Razor updaters", step.Description);
            Assert.Equal(WellKnownStepIds.RazorUpdaterStepId, step.Id);
            Assert.Equal("Update Razor files", step.Title);
            Assert.Collection(
                step.SubSteps.Select(s => s.Id),
                s => Assert.Equal("RazorUpdater #0", s),
                s => Assert.Equal("RazorUpdater #1", s),
                s => Assert.Equal("RazorUpdater #2", s));
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            Assert.False(step.IsDone);
            Assert.Throws<InvalidOperationException>(() => step.RazorDocuments);
        }

        [Fact]
        public void NegativeCtorTests()
        {
            Assert.Throws<ArgumentNullException>("razorUpdaters", () => new RazorUpdaterStep(null!, new NullLogger<RazorUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorUpdaterStep(Enumerable.Empty<IUpdater<RazorCodeDocument>>(), null!));
        }



        private AutoMock GetMock(int completeUpdaterCount, int incompleteUpdaterCount)
        {
            var mock = AutoMock.GetLoose(cfg =>
            {
                for (var i = 0; i < completeUpdaterCount + incompleteUpdaterCount; i++)
                {
                    var mock = new Mock<IUpdater<RazorCodeDocument>>();
                    mock.Setup(c => c.Id).Returns($"RazorUpdater #{i}");
                    mock.Setup(c => c.IsApplicableAsync(It.IsAny<IUpgradeContext>(),
                                                        It.IsAny<ImmutableArray<RazorCodeDocument>>(),
                                                        It.IsAny<CancellationToken>())).Returns(Task.FromResult(i >= completeUpdaterCount));
                    mock.Setup(c => c.Risk).Returns(BuildBreakRisk.Medium);
                    cfg.RegisterMock(mock);
                }
            });

            return mock;
        }
    }
}
