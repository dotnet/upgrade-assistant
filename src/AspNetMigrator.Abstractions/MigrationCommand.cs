using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public abstract class MigrationCommand
    {
        /// <summary>
        /// A command that can be executed.
        /// </summary>
        public abstract Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token);

        /// <summary>
        /// Gets the text displayed to the user from the REPL (e.g. Set Backup Path).
        /// </summary>
        public abstract string CommandText { get; }
    }
}
