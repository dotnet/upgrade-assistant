namespace AspNetMigrator
{
    public enum BuildBreakRisk
    {
        /// <summary>
        /// Unknown or inapplicable chance that a migration step will break a previously working build.
        /// </summary>
        Unknown,

        /// <summary>
        /// No chance that a migration step will break a previously working build.
        /// </summary>
        None,

        /// <summary>
        /// A low chance that a migration step will break a previously working build.
        /// </summary>
        Low,

        /// <summary>
        /// A medium chance that a migration step will break a previously working build.
        /// </summary>
        Medium,

        /// <summary>
        /// A high chance that a migration step will break a previously working build.
        /// </summary>
        High
    }
}
