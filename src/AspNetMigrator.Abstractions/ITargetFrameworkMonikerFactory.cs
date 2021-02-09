namespace AspNetMigrator
{
    public interface ITargetFrameworkMonikerFactory
    {
        TargetFrameworkMoniker GetTFMForNetFxVersion(string netFxVersion);
    }
}
