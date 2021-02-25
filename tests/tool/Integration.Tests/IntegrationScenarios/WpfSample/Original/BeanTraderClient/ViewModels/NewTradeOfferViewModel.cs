using BeanTrader.Models;
using System;
using System.Threading.Tasks;

namespace BeanTraderClient.ViewModels
{
    public class NewTradeOfferViewModel
    {
        private readonly Func<Task> closeDialogFunc;

        public BeanDictionary BeansOffered { get; } = new BeanDictionary();
        public BeanDictionary BeansAsked { get; } = new BeanDictionary();
        public event Func<TradeOffer, Task> CreateTradeHandler;

        public NewTradeOfferViewModel(Func<Task> closeDialogFunc)
        {
            this.closeDialogFunc = closeDialogFunc;
        }

        public async Task CreateTradeOfferAsync()
        {
            await closeDialogFunc().ConfigureAwait(false);

            await (CreateTradeHandler?.Invoke(new TradeOffer
            {
                Asking = BeansAsked,
                Offering = BeansOffered
            })).ConfigureAwait(false);
        }

        public async Task CancelTradeOfferAsync()
        {
            await closeDialogFunc().ConfigureAwait(false);
        }
    }
}
