// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    internal class TestOutputHelperLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public TestOutputHelperLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
            => new TestOutputHelperLogger(_output, categoryName);

        public void Dispose()
        {
        }

        private class TestOutputHelperLogger : ILogger, IDisposable
        {
            private readonly ITestOutputHelper _output;

            public TestOutputHelperLogger(ITestOutputHelper output, string name)
            {
                _output = output;

                Name = name;
            }

            public string Name { get; }

            public IDisposable BeginScope<TState>(TState state)
                => this;

            public void Dispose()
            {
            }

            public bool IsEnabled(LogLevel logLevel)
                => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var formatted = formatter(state, exception);

                _output.WriteLine($"[{logLevel}] {formatted}");

                if (exception is not null)
                {
                    _output.WriteLine(exception.ToString());
                }
            }
        }
    }
}
