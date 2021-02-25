using BeanTraderClient.DependencyInjection;
using BeanTraderClient.Views;
using MahApps.Metro;
using System;
using System.Configuration;
using System.IO;
using System.Windows;

namespace BeanTraderClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string AccentThemeExtension = ".Accent.xaml";
        private static readonly string ThemesRelativePath = Path.Combine("Resources", "Themes");

        protected override void OnStartup(StartupEventArgs e)
        {

            // Dynamically load a custom MahApps theme from disk as an exercise in using a few more Framework APIs
            var themesDirectory = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ThemesRelativePath);
            foreach (var accentFile in Directory.GetFiles(themesDirectory, $"*{AccentThemeExtension}"))
            {
                var fileName = Path.GetFileName(accentFile);
                ThemeManager.AddAccent(fileName.Substring(0, fileName.Length - AccentThemeExtension.Length), new Uri(accentFile));
            }

            // In the future, we could have multiple themes and store preferences in the registry
            var (currentTheme, _) = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current,
                ThemeManager.GetAccent(ConfigurationManager.AppSettings["DefaultTheme"]),
                currentTheme);

            Bootstrapper.Container.Resolve<MainWindow>().Show();

            base.OnStartup(e);
        }
    }
}
