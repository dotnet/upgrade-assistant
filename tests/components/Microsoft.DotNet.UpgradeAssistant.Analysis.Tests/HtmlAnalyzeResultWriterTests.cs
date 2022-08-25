// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using AutoFixture.Kernel;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis.Tests
{
    public class HtmlAnalyzeResultWriterTests
    {
        private readonly Fixture _fixture;

        public HtmlAnalyzeResultWriterTests()
        {
            _fixture = new Fixture();

            _fixture.Customizations.Add(new TypeRelay(typeof(IAsyncEnumerable<>), typeof(AsyncEnumerableImpl<>)));
        }

        [Fact]
        public async Task FailsWithNoSarif()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            using var ms = new MemoryStream();

            // Acct
            await mock.Create<HtmlAnalyzeResultWriter>().WriteAsync(AsyncEnumerable.Empty<OutputResultDefinition>(), ms, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(ms.ToArray());
        }

        [Fact]
        public async Task NoInput()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var sarifWriter = new Mock<IOutputResultWriter>().Object;

            mock.Mock<IOutputResultWriterProvider>().Setup(p => p.TryGetWriter("sarif", out sarifWriter)).Returns(true);

            using var ms = new MemoryStream();

            // Acct
            await mock.Create<HtmlAnalyzeResultWriter>().WriteAsync(AsyncEnumerable.Empty<OutputResultDefinition>(), ms, CancellationToken.None).ConfigureAwait(false);

            // Assert
            // Attempt to move position to ensure stream is still open
            ms.Position = 0;

            Assert.NotEmpty(ms.ToArray());
        }

        [Fact]
        public async Task ValidateSarifInjected()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var sarifWriter = new Mock<IOutputResultWriter>();
            var sarifWriterObject = sarifWriter.Object;

            mock.Mock<IOutputResultWriterProvider>().Setup(p => p.TryGetWriter("sarif", out sarifWriterObject)).Returns(true);

            var definitions = _fixture.CreateMany<OutputResultDefinition>().ToAsyncEnumerable();

            using var ms = new MemoryStream();

            // Act
            await mock.Create<HtmlAnalyzeResultWriter>().WriteAsync(definitions, ms, CancellationToken.None).ConfigureAwait(false);

            // Assert
            sarifWriter.Verify(w => w.WriteAsync(definitions, It.IsAny<Stream>(), default), Times.Once);

            // Attempt to move position to ensure stream is still open
            ms.Position = 0;

            Assert.NotEmpty(ms.ToArray());
        }

        private class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                yield break;
            }
        }
    }
}
