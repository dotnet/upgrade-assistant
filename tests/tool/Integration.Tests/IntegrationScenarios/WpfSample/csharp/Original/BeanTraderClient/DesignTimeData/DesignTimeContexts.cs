using BeanTrader.Models;
using BeanTraderClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace BeanTraderClient.DesignTimeData
{
    public static class DesignTimeContexts
    {
        public static TradingViewModel DesignTimeTradingViewModel =>
            new TradingViewModel(null, null, null)
            {
                CurrentTrader = new Trader
                {
                    Name = "Test User",
                    Id = Guid.Empty,
                    Inventory = new[] { 100, 50, 10, 1 }
                },
                TradeOffers = new List<TradeOffer>
                {
                    new TradeOffer
                    {
                        Id = Guid.Empty,
                        SellerId = Guid.Empty,
                        Asking = new Dictionary<Beans, uint>
                        {
                            { Beans.Red, 5 },
                            { Beans.Blue, 5 }
                        },
                        Offering = new Dictionary<Beans, uint>
                        {
                            { Beans.Yellow, 1 }
                        }
                    },
                    new TradeOffer
                    {
                        Id = Guid.Empty,
                        SellerId = Guid.Empty,
                        Asking = new Dictionary<Beans, uint>
                        {
                            { Beans.Green, 20 }
                        },
                        Offering = new Dictionary<Beans, uint>
                        {
                            { Beans.Red, 10 },
                            { Beans.Yellow, 1 },
                            { Beans.Blue, 5 },
                            { Beans.Green, 10 }
                        }
                    }
                },
                StatusText = "Test message",
                StatusBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0))
            };
    }
}
