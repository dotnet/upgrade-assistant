using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class PackageUpdaterStepExtensions
    {
        public static OptionsBuilder<PackageUpdaterOptions> AddPackageUpdaterStep(this IServiceCollection services)
        {
            services.AddSingleton<PackageMapProvider>();
            services.AddSingleton<IPackageLoader, PackageLoader>();
            services.AddScoped<MigrationStep, PackageUpdaterStep>();
            return services.AddOptions<PackageUpdaterOptions>();
        }
    }
}
