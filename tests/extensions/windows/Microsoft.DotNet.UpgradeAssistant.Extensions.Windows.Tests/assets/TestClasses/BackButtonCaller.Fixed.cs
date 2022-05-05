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
              
            TODO UA3015 Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button "UAGeneratedBackButton" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015
            */
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private async void SetBackButtonVisibility()
        {
            /*
              
            TODO UA3015 Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button "UAGeneratedBackButton" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015
            */
            UAGeneratedBackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            /*
              
            TODO UA3015 Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button "UAGeneratedBackButton" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015
            */
            UAGeneratedBackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        private async void SetBackButtonVisibility2()
        {
            var x = SystemNavigationManager.GetForCurrentView();
            /*
              
            TODO UA3015 Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button "UAGeneratedBackButton" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015
            */
            UAGeneratedBackButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        private async void SetBackButtonVisibility3()
        {
            var x = SystemNavigationManager.GetForCurrentView();
            /*
              
            TODO UA3015 Default back button in the title bar does not exist in WinUI3 apps.
            The tool should have generated a custom back button "UAGeneratedBackButton" in the XAML file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://aka.ms/UWP.NetUpgrade/UA3015
            */
            x.AppViewBackButtonVisibility = Hello();
        }
    }
}
