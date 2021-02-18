using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Reporting;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class ProjectFormatStepModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SetTFMStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<TryConvertProjectConverterStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<TryConvertReport>().As<ISectionGenerator>();
        }
    }
}
