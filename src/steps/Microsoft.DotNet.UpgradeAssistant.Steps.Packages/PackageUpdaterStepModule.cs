using System.Linq;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageUpdaterStepModule : Module
    {
        private const string PackageUpdaterOptionsSectionName = "PackageUpdater";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PackageUpdaterStep>().As<MigrationStep>().InstancePerLifetimeScope();
            builder.RegisterType<PackageMapProvider>().SingleInstance();
            builder.Register(context =>
            {
                var config = context.Resolve<IConfiguration>();
                return config.GetSection(PackageUpdaterOptionsSectionName).Get<PackageUpdaterOptions>();
            });

            // Although this only registers analyzers from the default extension,
            // other extensions can register additional analyzers in their own modules.
            builder.RegisterAssemblyTypes(typeof(DuplicateReferenceAnalyzer).Assembly)
                .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IPackageReferencesAnalyzer)))
                .As<IPackageReferencesAnalyzer>();
        }
    }
}
