namespace AspNetMigrator.Engine
{
    public class UserMessage
    {
        public string Message { get; set; } = string.Empty;

        public MessageSeverity Severity { get; set; }
    }
}
