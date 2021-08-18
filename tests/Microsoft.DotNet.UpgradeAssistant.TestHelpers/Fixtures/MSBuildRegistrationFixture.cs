// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.DotNet.UpgradeAssistant.Fixtures
{
    public class MSBuildRegistrationFixture : IDisposable
    {
        private readonly List<string> _messages = new();

        public MSBuildRegistrationFixture()
        {
            Mock = AutoMock.GetLoose(b =>
            {
                b.RegisterGeneric(typeof(TestLogger<>)).As(typeof(ILogger<>));
                b.RegisterInstance(_messages);
                b.RegisterType<ProcessRunner>().As<IProcessRunner>();
            });

            var options = new WorkspaceOptions();

            Mock.Create<MSBuildWorkspaceOptionsConfigure>().Configure(options);
            Mock.Mock<IOptions<WorkspaceOptions>>().Setup(o => o.Value).Returns(options);

            var msBuildRegistrar = Mock.Create<MSBuildRegistrationStartup>();

            msBuildRegistrar.Register();
        }

        public AutoMock Mock { get; }

        public IEnumerable<string> Messages => _messages;

        public void Dispose()
        {
            Mock.Dispose();
        }

        private class TestLogger<T> : ILogger<T>
        {
            private readonly List<string> _messages;

            public TestLogger(List<string> messages)
            {
                _messages = messages;
            }

            public IDisposable BeginScope<TState>(TState state)
                => new Mock<IDisposable>().Object;

            public bool IsEnabled(LogLevel logLevel)
                => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _messages.Add($"{logLevel}: {formatter(state, exception)}");
            }
        }
    }
}
