using System.Linq;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigUpdaterStepModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConfigUpdaterStep>().As<MigrationStep>().InstancePerLifetimeScope();

            // Although this only registers config updaters from the default extension,
            // other extensions can register additional updaters in their own modules.
            builder.RegisterAssemblyTypes(typeof(AppSettingsMigrator).Assembly)
                .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IConfigUpdater)))
                .As<IConfigUpdater>();
        }
    }
}
