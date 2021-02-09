using System;

namespace Microsoft.UpgradeAssistant
{
    public class MigrationException : Exception
    {
        public MigrationException()
        {
        }

        public MigrationException(string message)
            : base(message)
        {
        }

        public MigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
