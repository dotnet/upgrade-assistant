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
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        }

        private async void SetBackButtonVisibility()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private async void SetBackButtonVisibility2()
        {
            var x = SystemNavigationManager.GetForCurrentView();
            x.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private async void SetBackButtonVisibility3()
        {
            var x = SystemNavigationManager.GetForCurrentView();
            x.AppViewBackButtonVisibility = Hello();
        }
    }
}
