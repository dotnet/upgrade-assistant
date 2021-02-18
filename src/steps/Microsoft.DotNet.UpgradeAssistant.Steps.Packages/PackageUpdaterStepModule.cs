using System.Linq;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;

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
                var extensionProvider = context.Resolve<AggregateExtensionProvider>();

                // Read the package updater options from all extensions.
                // Alternatively, if we wanted to just get options from this extension,
                // we could filter extensionProvider.ExtensionProviders by name to get
                // this particular extension and read configuration from there.
                return extensionProvider.GetOptions<PackageUpdaterOptions>(PackageUpdaterOptionsSectionName)
                    ?? new PackageUpdaterOptions();
            });

            // Although this only registers analyzers from the default extension,
            // other extensions can register additional analyzers in their own modules.
            builder.RegisterAssemblyTypes(typeof(DuplicateReferenceAnalyzer).Assembly)
                .Where(t => t.IsPublic && t.IsAssignableTo(typeof(IPackageReferencesAnalyzer)))
                .As<IPackageReferencesAnalyzer>();
        }
    }
}
