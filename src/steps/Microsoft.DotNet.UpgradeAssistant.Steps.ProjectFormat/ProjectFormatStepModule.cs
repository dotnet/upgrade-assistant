using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Reporting;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class ProjectFormatStepModule : Module
    {
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvertProjectConverter";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SetTFMStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<TryConvertProjectConverterStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<TryConvertReport>().As<ISectionGenerator>();

            builder.Register(context =>
            {
                var extensionProvider = context.Resolve<AggregateExtensionProvider>();

                // Read the try-convert updater options from all extensions.
                // Alternatively, if we wanted to just get options from this extension,
                // we could filter extensionProvider.ExtensionProviders by name to get
                // this particular extension and read configuration from there.
                return extensionProvider.GetOptions<TryConvertProjectConverterStepOptions>(TryConvertProjectConverterStepOptionsSection)
                    ?? new TryConvertProjectConverterStepOptions();
            });
        }
    }
}
