namespace AspNetMigrator.TemplateUpdater
{
    /// <summary>
    /// An item to be added to a project.
    /// </summary>
    public record ItemSpec(string Type, string Path, bool IncludeExplicitly, string[] Keywords);
}
