using Autofac;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateInserterStepModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TemplateInserterStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<TemplateProvider>().SingleInstance();
        }
    }
}
