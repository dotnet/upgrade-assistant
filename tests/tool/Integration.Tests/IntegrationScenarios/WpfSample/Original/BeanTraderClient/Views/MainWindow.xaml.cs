using MahApps.Metro.Controls;

namespace BeanTraderClient.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow(WelcomePage welcomePage)
        {
            InitializeComponent();

            MainFrame.Navigate(welcomePage);
        }
    }
}
