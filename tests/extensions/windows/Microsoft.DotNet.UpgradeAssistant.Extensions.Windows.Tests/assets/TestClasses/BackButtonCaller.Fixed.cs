using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace TestProject.TestClasses
{
    public class BackButtonCaller
    {
        private async void CallBackButton()
        {

            /*
              
            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private async void SetBackButtonVisibility()
        {

            /*
              
            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */App.Window.BackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            /*
              
            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */App.Window.BackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;        }

        private async void SetBackButtonVisibility2()
        {
            var x = SystemNavigationManager.GetForCurrentView();

            /*
              
            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */App.Window.BackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;        }

        private async void SetBackButtonVisibility3()
        {
            var x = SystemNavigationManager.GetForCurrentView();

            /*
              
            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */x.AppViewBackButtonVisibility = Hello();
        }
    }
}
