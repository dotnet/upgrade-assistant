using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using System.Windows;
using System.Windows.Controls;

namespace BeanTraderClient.DependencyInjection
{
    public class ViewInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromThisAssembly()
                .BasedOn<Window>()
                .OrBasedOn(typeof(Page))
                .LifestyleTransient());
        }
    }
}
