using Serilog;
using Serilog.Events;

namespace AspNetMigrator.ConsoleApp
{
    internal class ConsoleLogger: Engine.ILogger
    {
        ILogger Logger { get; }

        public ConsoleLogger(bool verbose) =>
            Logger = new LoggerConfiguration()
                .WriteTo.Console(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                .CreateLogger();

        public void Error(string messageTemplate, params object[] propertyValues) => Logger.Error(messageTemplate, propertyValues);

        public void Fatal(string messageTemplate, params object[] propertyValues) => Logger.Fatal(messageTemplate, propertyValues);

        public void Information(string messageTemplate, params object[] propertyValues) => Logger.Information(messageTemplate, propertyValues);

        public void Verbose(string messageTemplate, params object[] propertyValues) => Logger.Verbose(messageTemplate, propertyValues);

        public void Warning(string messageTemplate, params object[] propertyValues) => Logger.Warning(messageTemplate, propertyValues);
    }
}
