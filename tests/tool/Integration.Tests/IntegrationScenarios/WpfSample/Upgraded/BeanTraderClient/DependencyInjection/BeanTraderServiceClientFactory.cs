using BeanTrader;
using BeanTrader.Models;
using System.ServiceModel;

namespace BeanTraderClient.DependencyInjection
{
    public class BeanTraderServiceClientFactory
    {
        private IBeanTraderCallback CallbackHandler { get; }

        public BeanTraderServiceClientFactory(IBeanTraderCallback callbackHandler)
        {
            CallbackHandler = callbackHandler;
        }

        public BeanTraderServiceClient GetServiceClient() => new BeanTraderServiceClient(new InstanceContext(CallbackHandler));
    }
}