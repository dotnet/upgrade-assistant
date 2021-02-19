using Microsoft.DotNet.UpgradeAssistant.Steps.Solution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class SolutionStepsExtensions
    {
        public static IServiceCollection AddSolutionSteps(this IServiceCollection services) =>
            services.AddScoped<MigrationStep, CurrentProjectSelectionStep>()
                .AddScoped<MigrationStep, EntrypointSelectionStep>();

    }
}
