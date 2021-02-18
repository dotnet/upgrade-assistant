using Autofac;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class SolutionStepsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CurrentProjectSelectionStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<EntrypointSelectionStep>().As<MigrationStep>().InstancePerLifetimeScope();
        }
    }
}
