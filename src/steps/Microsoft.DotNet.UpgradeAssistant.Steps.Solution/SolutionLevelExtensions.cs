using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public static class SolutionLevelExtensions
    {
        public static void AddSolutionLevelSteps(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, CurrentProjectSelectionStep>();
            services.AddScoped<MigrationStep, EntrypointSelectionStep>();
        }
    }
}
