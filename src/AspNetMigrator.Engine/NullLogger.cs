namespace AspNetMigrator.Engine
{
    public class NullLogger : ILogger
    {
        public void Error(string messageTemplate, params object[] propertyValues) { }

        public void Fatal(string messageTemplate, params object[] propertyValues) { }

        public void Information(string messageTemplate, params object[] propertyValues) { }

        public void Verbose(string messageTemplate, params object[] propertyValues) { }

        public void Warning(string messageTemplate, params object[] propertyValues) { }
    }
}
