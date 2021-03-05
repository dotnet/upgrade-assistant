using BeanTrader;
using BeanTraderClient.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using MahApps.Metro.Controls.Dialogs;

namespace BeanTraderClient.DependencyInjection
{
    public class ServiceInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // MahApps dialog coordinator
            container.Register(Component.For<IDialogCoordinator>().Instance(DialogCoordinator.Instance));

            // BeanTrader services
            container.Register(Component.For<IBeanTraderCallback, BeanTraderCallback>()
                .ImplementedBy<BeanTraderCallback>()
                .LifestyleSingleton());
            container.Register(Component.For<BeanTraderServiceClientFactory>());
            container.Register(Component.For<BeanTraderServiceClient>()
                .UsingFactory<BeanTraderServiceClientFactory, BeanTraderServiceClient>(factory => factory.GetServiceClient()));
            container.Register(Component.For<TradingService>()
                .LifestyleSingleton());
        }
    }
}
