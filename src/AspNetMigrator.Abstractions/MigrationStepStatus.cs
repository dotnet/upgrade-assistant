namespace AspNetMigrator
{
    public enum MigrationStepStatus
    {
        /// <summary>
        /// A migration step in an unknown state, possibly uninitialized.
        /// </summary>
        Unknown,

        /// <summary>
        /// A migration step that is initialized but has not completed.
        /// </summary>
        Incomplete,

        /// <summary>
        /// A migration step that has run successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// A migration step that was initialized and then skipped.
        /// </summary>
        Skipped,

        /// <summary>
        /// A migration step that ran and failed while running.
        /// </summary>
        Failed
    }
}
