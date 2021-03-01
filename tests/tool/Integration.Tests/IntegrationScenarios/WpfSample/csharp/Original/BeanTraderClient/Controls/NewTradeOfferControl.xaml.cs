using BeanTraderClient.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BeanTraderClient.Controls
{
    /// <summary>
    /// Interaction logic for NewTradeOfferControl.xaml
    /// </summary>
    public partial class NewTradeOfferControl : UserControl
    {
        NewTradeOfferViewModel Model => DataContext as NewTradeOfferViewModel;

        public NewTradeOfferControl()
        {
            InitializeComponent();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e) => await Model.CancelTradeOfferAsync().ConfigureAwait(false);
        private async void CreateButton_Click(object sender, RoutedEventArgs e) => await Model.CreateTradeOfferAsync().ConfigureAwait(false);

        // Select text in the bean count text boxes so that users can easily replace the initial '0'
        private void BeanTextBox_GotFocus(object sender, EventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void IgnoreIfUnfocused(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }
    }
}
