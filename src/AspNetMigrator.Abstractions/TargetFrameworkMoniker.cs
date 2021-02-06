namespace AspNetMigrator
{
    public record TargetFrameworkMoniker(string Name)
    {
        public override string ToString() => Name;
    }
}
