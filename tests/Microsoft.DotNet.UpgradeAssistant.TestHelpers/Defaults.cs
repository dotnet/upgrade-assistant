using System.IO;

namespace Microsoft.UpgradeAssistant
{
    public static class Defaults
    {
        public static MigrateOptions DefaultMigrateOptions =>
            new()
            {
                // Migrate options are only valid if their Project property points at a file that exists
                Project = new FileInfo(typeof(Defaults).Assembly.Location)
            };
    }
}
