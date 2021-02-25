using BeanTrader.Models;
using BeanTraderClient.Controls;
using BeanTraderClient.Resources;
using BeanTraderClient.Services;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BeanTraderClient.ViewModels
{
    public class TradingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Trader trader;
        private string statusText;
        private Brush statusBrush;
        private string userName;
        private IList<TradeOffer> tradeOffers;

        private IDialogCoordinator DialogCoordinator { get; }
        private TradingService TradingService { get; }
        private BeanTraderCallback CallbackHandler { get; }
        private Timer StatusClearTimer { get; }

        // Initialized by ListenForTradeOffers, this field caches trader names (indexed by ID)
        private ConcurrentDictionary<Guid, string> traderNames;

        public TradingViewModel(IDialogCoordinator dialogCoordinator, TradingService tradingService, BeanTraderCallback callbackHandler)
        {
            DialogCoordinator = dialogCoordinator;
            TradingService = tradingService;
            CallbackHandler = callbackHandler;
            StatusClearTimer = new Timer(ClearStatus);
            traderNames = new ConcurrentDictionary<Guid, string>();
        }

        public async Task LoadAsync()
        {
            TradingService.Connected += LoadDataAsync;

            // Get initial trader info and trade offers
            await LoadDataAsync().ConfigureAwait(false);

            // Register for service callbacks
            CallbackHandler.AddNewTradeOfferHandler += AddTradeOffer;
            CallbackHandler.RemoveTradeOfferHandler += RemoveTraderOffer;
            CallbackHandler.TradeAcceptedHandler += TradeAccepted;
        }

        public async Task UnloadAsync()
        {
            // Stop listening
            await TradingService.StopListeningAsync().ConfigureAwait(false);
            await TradingService.LogoutAsync().ConfigureAwait(false);

            // Unregister for service callbacks
            CallbackHandler.AddNewTradeOfferHandler -= AddTradeOffer;
            CallbackHandler.RemoveTradeOfferHandler -= RemoveTraderOffer;
            CallbackHandler.TradeAcceptedHandler -= TradeAccepted;

            // Clear model data
            CurrentTrader = null;
            TradeOffers = null;
        }

        public Trader CurrentTrader
        {
            get => trader;
            set
            {
                if (trader != value)
                {
                    trader = value;
                    OnPropertyChanged(nameof(UserName));
                    OnPropertyChanged(nameof(Inventory));
                    OnPropertyChanged(nameof(WelcomeMessage));
                }
            }
        }

        public string StatusText
        {
            get => statusText;
            set
            {
                if (statusText != value)
                {
                    statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }
        
        public async Task<string> GetTraderNameAsync(Guid sellerId)
        {
            // Until we add a design-time trading service mock, just bail out on null TradingService
            if (TradingService == null)
            {
                return sellerId.ToString();
            }

            if (!traderNames.TryGetValue(sellerId, out string traderName))
            {
                var names = await TradingService.GetTraderNamesAsync(new Guid[] { sellerId }).ConfigureAwait(false);

                traderName = names.ContainsKey(sellerId) ?
                    traderNames.AddOrUpdate(sellerId, names[sellerId], (g, s) => names[sellerId]) :
                    null;
            }

            return traderName;
        }

        public Brush StatusBrush
        {
            get => statusBrush;
            set
            {
                if (statusBrush != value)
                {
                    statusBrush = value;
                    OnPropertyChanged(nameof(StatusBrush));
                }
            }
        }

#pragma warning disable CA2227 // Collection properties should be read only
        public IList<TradeOffer> TradeOffers
#pragma warning restore CA2227 // Collection properties should be read only
        {
            get => tradeOffers;
            set
            {
                tradeOffers = value;
                OnPropertyChanged(nameof(TradeOffers));
            }
        }

        public string UserName
        {
            get => userName;
            set
            {
                userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }
        
        public IEnumerable<int> Inventory => CurrentTrader?.Inventory ?? new int[4];

        public string WelcomeMessage =>
            string.IsNullOrEmpty(UserName) ?
            StringResources.DefaultGreeting :
            string.Format(CultureInfo.CurrentCulture, StringResources.Greeting, UserName);

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateTraderInfo()
        {
            // As an example, do work asynchronously with Delegate.BeginInvoke to demonstrate
            // how such calls can be ported to .NET Core.
            Func<Task<Trader>> userInfoRetriever = TradingService.GetCurrentTraderInfoAsync;
            userInfoRetriever.BeginInvoke(result =>
            {
                var task = userInfoRetriever.EndInvoke(result).ConfigureAwait(false);
                CurrentTrader = task.GetAwaiter().GetResult();
            }, null);
        }

        private async Task LoadDataAsync()
        {
            await LoginAsync().ConfigureAwait(false);
            UpdateTraderInfo();
            await ListenForTradeOffersAsync().ConfigureAwait(false);
        }

        private Task LoginAsync()
        {
            return TradingService.LoginAsync(UserName);
        }

        private Task ListenForTradeOffersAsync()
        {
            // Different async pattern just for demonstration's sake
            return TradingService.ListenForTradeOffersAsync()
                .ContinueWith(async offersTask =>
                {
                    var tradeOffers = await offersTask ?? Array.Empty<TradeOffer>();
                    var sellerIds = tradeOffers?.Select(t => t.SellerId);
                    traderNames = new ConcurrentDictionary<Guid, string>(await TradingService.GetTraderNamesAsync(sellerIds.ToArray()));
                    TradeOffers = new ObservableCollection<TradeOffer>(tradeOffers);
                }, CancellationToken.None, TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default);
        }

        private void RemoveTraderOffer(Guid offerId)
        {
            var offer = tradeOffers.SingleOrDefault(o => o.Id == offerId);
            if (offer != null)
            {
                Application.Current.Dispatcher.Invoke(() => TradeOffers.Remove(offer));
            }
        }

        private void AddTradeOffer(TradeOffer offer)
        {
            if (!tradeOffers.Any(o => o.Id == offer.Id))
            {
                Application.Current.Dispatcher.Invoke(() => TradeOffers.Add(offer));
            }
        }

        private void TradeAccepted(TradeOffer offer, Guid buyerId)
        {
            if (offer.SellerId == CurrentTrader.Id)
            {
                Task.Run(async () =>
                {
                    SetStatus($"Trade ({offer}) accepted by {await GetTraderNameAsync(buyerId) ?? buyerId.ToString()}");
                    UpdateTraderInfo();
                });
            }
        }

        public async Task<bool> CompleteTrade(TradeOffer tradeOffer)
        {
            var ownTrade = tradeOffer.SellerId == CurrentTrader.Id;
            var success = ownTrade ?
                await TradingService.CancelTradeOfferAsync(tradeOffer.Id).ConfigureAwait(false) :
                await TradingService.AcceptTradeAsync(tradeOffer.Id).ConfigureAwait(false);

            if (success)
            {
                SetStatus($"{(ownTrade ? "Canceled" : "Accepted")} trade ({tradeOffer})");
                UpdateTraderInfo();
            }
            else
            {
                SetStatus($"Unable to {(ownTrade ? "cancel" : "accept")} trade ({tradeOffer}).", Application.Current.FindResource("ErrorBrush") as Brush);
            }

            return success;
        }

        public async Task ShowNewTradeOfferDialog()
        {
            var newTradeDialog = new CustomDialog
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Application.Current.FindResource("WindowBackgroundBrush") as Brush,
                Style = Application.Current.FindResource("DefaultControlStyle") as Style
            };

            var newTradeOfferViewModel = new NewTradeOfferViewModel(() => DialogCoordinator.HideMetroDialogAsync(this, newTradeDialog));
            newTradeOfferViewModel.CreateTradeHandler += CreateTradeOfferAsync;

            newTradeDialog.Content = new NewTradeOfferControl
            {
                DataContext = newTradeOfferViewModel
            };

            await DialogCoordinator.ShowMetroDialogAsync(this, newTradeDialog).ConfigureAwait(false);
        }

        private async Task CreateTradeOfferAsync(TradeOffer tradeOffer)
        {
            if (await TradingService.OfferTradeAsync(tradeOffer).ConfigureAwait(false) != Guid.Empty)
            {
                SetStatus("New trade offer created");
            }
            else
            {
                SetStatus("ERROR: Trade offer could not be created. Do you have enough beans?", Application.Current.FindResource("ErrorBrush") as Brush);
            }

            UpdateTraderInfo();
        }

        private void SetStatus(string message) => SetStatus(message, Application.Current.FindResource("IdealForegroundColorBrush") as Brush);

        private void SetStatus(string message, Brush brush)
        {
            StatusText = message;
            StatusBrush = brush;
            ResetStatusClearTimer();
        }

        private void ResetStatusClearTimer(int dueTime = 5000)
        {
            StatusClearTimer.Change(dueTime, Timeout.Infinite);
        }

        private void ClearStatus(object _)
        {
            StatusText = string.Empty;
        }
    }
}
