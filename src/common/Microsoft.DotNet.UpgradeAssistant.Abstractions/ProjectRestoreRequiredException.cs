using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class ProjectRestoreRequiredException : MigrationException
    {
        public ProjectRestoreRequiredException()
        {
        }

        public ProjectRestoreRequiredException(string message)
            : base(message)
        {
        }

        public ProjectRestoreRequiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
