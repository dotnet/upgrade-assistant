using Castle.Windsor;
using Castle.Windsor.Installer;

namespace BeanTraderClient.DependencyInjection
{
    public static class Bootstrapper
    {
        private static IWindsorContainer container;
        private static readonly object syncRoot = new object();

        public static IWindsorContainer Container
        {
            get
            {
                if (container == null)
                {
                    lock (syncRoot)
                    {
                        if (container == null)
                        {
                            container = new WindsorContainer().Install(FromAssembly.This());
                        }
                    }
                }

                return container;
            }
        }
    }
}
