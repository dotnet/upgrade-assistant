using Microsoft.DotNet.UpgradeAssistant.Steps.Backup;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class BackupStepExtensions
    {
        public static IServiceCollection AddBackupStep(this IServiceCollection services) =>
            services.AddScoped<MigrationStep, BackupStep>();
    }
}
