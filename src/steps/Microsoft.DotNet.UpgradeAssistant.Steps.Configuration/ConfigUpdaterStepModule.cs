using System.Linq;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigUpdaterStepModule : Module
    {
        private const string ConfigUpdaterOptionsSectionName = "ConfigUpdater";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConfigUpdaterStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.Register(context =>
            {
                var extensionProvider = context.Resolve<AggregateExtensionProvider>();

                // Read the config updater options from all extensions.
                // Alternatively, if we wanted to just get options from this extension,
                // we could filter extensionProvider.ExtensionProviders by name to get
                // this particular extension and read configuration from there.
                return extensionProvider.GetOptions<ConfigUpdaterOptions>(ConfigUpdaterOptionsSectionName)
                    ?? new ConfigUpdaterOptions();
            });

            // Although this only registers config updaters from the default extension,
            // other extensions can register additional updaters in their own modules.
            builder.RegisterAssemblyTypes(typeof(AppSettingsMigrator).Assembly)
                .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IConfigUpdater)))
                .As<IConfigUpdater>();
        }
    }
}
