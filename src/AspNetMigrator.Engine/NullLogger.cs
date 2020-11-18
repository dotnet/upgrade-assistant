using System;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Engine
{
    public class NullLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }

        public void Error(string messageTemplate, params object[] propertyValues) { }

        public void Fatal(string messageTemplate, params object[] propertyValues) { }

        public void Information(string messageTemplate, params object[] propertyValues) { }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        public void Verbose(string messageTemplate, params object[] propertyValues) { }

        public void Warning(string messageTemplate, params object[] propertyValues) { }
    }
}
