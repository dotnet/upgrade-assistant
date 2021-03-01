using BeanTrader.Models;
using BeanTraderClient.Resources;
using BeanTraderClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BeanTraderClient.Controls
{
    /// <summary>
    /// Interaction logic for TradeOfferControl.xaml
    /// </summary>
    public partial class TradeOfferControl : UserControl
    {
        public static readonly DependencyProperty TradeOfferProperty =
            DependencyProperty.Register("TradeOffer", typeof(TradeOffer), typeof(TradeOfferControl), new PropertyMetadata(new PropertyChangedCallback(UpdateTradeOffer)));

        public static readonly DependencyProperty TradingModelProperty =
            DependencyProperty.Register("TradingModel", typeof(TradingViewModel), typeof(TradeOfferControl));

        public static readonly DependencyProperty SellerNameProperty =
            DependencyProperty.Register("SellerName", typeof(string), typeof(TradeOfferControl));

        private static async void UpdateTradeOffer(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Update these on TradeOffer so that the BeanDictionaries are available for
            // data binding without having to construct them each time they're needed
            if (e.NewValue is TradeOffer newTradeOffer && d is TradeOfferControl control)
            {
                control.Offering = new BeanDictionary(newTradeOffer.Offering);
                control.Asking = new BeanDictionary(newTradeOffer.Asking);

                // Temporary name while looking up the friendly name
                control.SellerName = newTradeOffer.SellerId.ToString();
                if (control.TradingModel != null)
                {
                    control.SellerName = await control.TradingModel.GetTraderNameAsync(newTradeOffer.SellerId).ConfigureAwait(true);
                }
            }
        }


        private async void TradeOfferControl_Loaded(object sender, RoutedEventArgs e)
        {
            var sellerId = TradeOffer?.SellerId.ToString();

            // If the seller name hasn't been loaded yet, look it up
            if (sellerId != null && sellerId == SellerName && TradingModel != null)
            {
                SellerName = await TradingModel.GetTraderNameAsync(TradeOffer.SellerId).ConfigureAwait(true);
            }
        }

        public TradeOffer TradeOffer
        {
            get => (TradeOffer)GetValue(TradeOfferProperty);
            set => SetValue(TradeOfferProperty, value);
        }

        // Access parent trading model (if any) to allow making trades, etc.
        public TradingViewModel TradingModel
        {
            get => (TradingViewModel)GetValue(TradingModelProperty);
            set => SetValue(TradingModelProperty, value);
        }

        public string SellerName
        {
            get => (string)GetValue(SellerNameProperty);
            set => SetValue(SellerNameProperty, value);
        }

        public BeanDictionary Offering { get; private set; }

        public BeanDictionary Asking { get; private set; }

        public string CompleteTradeDescription =>
            TradeOffer?.SellerId == TradingModel?.CurrentTrader.Id ?
            StringResources.CancelTradeDescription :
            StringResources.AcceptTradeDescription;

        public TradeOfferControl()
        {
            InitializeComponent();

            // Set LayoutRoot's data context so that data binding within this
            // control uses this object as the context but data binding by 
            // this control's users won't have their context's changed.
            LayoutRoot.DataContext = this;
        }

        private async void CompleteTradeButton_Click(object sender, RoutedEventArgs e)
        {
            CompleteTradeButton.IsEnabled = false;
            if (!await (TradingModel?.CompleteTrade(TradeOffer)).ConfigureAwait(true))
            {
                CompleteTradeButton.IsEnabled = true;
            }            
        }
    }
}
