using BeanTraderClient.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BeanTraderClient.Views
{
    /// <summary>
    /// Interaction logic for TradingPage.xaml
    /// </summary>
    public partial class TradingPage : Page
    {
        public TradingViewModel Model { get; }

        public TradingPage(TradingViewModel viewModel)
        {
            InitializeComponent();
            Model = viewModel;
            this.DataContext = this.Model;
        }

        private async void Load(object sender, RoutedEventArgs e)
        {
            // Make sure that this page's model is
            // cleaned up if the app closes
            Application.Current.MainWindow.Closing += Unload;

            await Model.LoadAsync().ConfigureAwait(false);
        }

        private async void Unload(object sender, EventArgs e)
        {
            Application.Current.MainWindow.Closing -= Unload;

            // In the case of the app closing, it's possible that 
            // UnloadAsync won't have a chance to finish.
            // I think it's better to preserve the 'fast exit' user
            // experience and just harden the backend against possible
            // abandoned sessions.
            // The alternative would be to wrap this in Task.Run and wait
            // for it to finish.
            await Model.UnloadAsync().ConfigureAwait(false);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        
        private async void NewTradeButton_Click(object sender, RoutedEventArgs e)
        {
            await Model.ShowNewTradeOfferDialog().ConfigureAwait(false);
        }
    }
}
