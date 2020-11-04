using Serilog;
using Serilog.Events;

namespace AspNetMigrator.ConsoleApp
{
    internal class ConsoleLogger : Engine.ILogger
    {
        private ILogger Logger { get; }

        public ConsoleLogger(bool verbose) =>
            Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(verbose ? LogEventLevel.Verbose : LogEventLevel.Information)
                .CreateLogger();

        public void Error(string messageTemplate, params object[] propertyValues) => Logger.Error(messageTemplate, propertyValues);

        public void Fatal(string messageTemplate, params object[] propertyValues) => Logger.Fatal(messageTemplate, propertyValues);

        public void Information(string messageTemplate, params object[] propertyValues) => Logger.Information(messageTemplate, propertyValues);

        public void Verbose(string messageTemplate, params object[] propertyValues) => Logger.Verbose(messageTemplate, propertyValues);

        public void Warning(string messageTemplate, params object[] propertyValues) => Logger.Warning(messageTemplate, propertyValues);
    }
}
