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
    // This class is unused. It's not even part of the BeanTraderClient project.
    // It exists here as a demonstrating of how old, unused source can introduce errors when
    // porting to .NET Core since the new project system defaults to including all source
    // files under its directory.
    public class TradingViewModel2 : INotifyPropertyChanged
    {
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

        public TradingViewModel2(IDialogCoordinator dialogCoordinator, TradingService tradingService, BeanTraderCallback callbackHandler)
        {
            DialogCoordinator = dialogCoordinator;
            TradingService = tradingService;
            CallbackHandler = callbackHandler;
            StatusClearTimer = new Timer(ClearStatus);
            traderNames = new ConcurrentDictionary<Guid, string>();
        }
    }
}
