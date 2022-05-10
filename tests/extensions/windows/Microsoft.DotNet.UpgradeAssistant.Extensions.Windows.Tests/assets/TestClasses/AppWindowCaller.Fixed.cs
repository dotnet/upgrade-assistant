using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.UI.WindowManagement;

namespace TestProject.TestClasses
{
    public class AppWindowCaller
    {
        private async void CreateNewWindow()
        {
Microsoft.UI.Windowing.AppWindow newWindow = 
                Microsoft.UI.Windowing.AppWindow.Create()
;

                newWindow.Show()
;
        }

        private 
                /*
                   TODO: Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow GetWindow()
        {
            var x = ABC.AppWindow;
            return 
                Microsoft.UI.Windowing.AppWindow.Create()
;
        }

        private void Other()
        {
            Windows.UI.WindowManagement.AppWindowChangedEventArgs x;
            x.DidSizeChange += () => { };
        }
    }
}
