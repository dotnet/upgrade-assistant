using Autofac;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Backup
{
    public class BackupStepModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BackupStep>().As<MigrationStep>().InstancePerLifetimeScope();
        }
    }
}
