using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.UI.WindowManagement;
using Windows.UI.ViewManagement;

namespace TestProject.TestClasses
{
    public class AppWindowCaller
    {
        private async void CreateNewWindow()
        {
Microsoft.UI.Windowing.AppWindow newWindow = 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow.Create();
newWindow.Show();

            var x = 
                    /* 
                        TODO UA315_B
                        Use Microsoft.UI.Windowing.AppWindow.Position instead of GetPlacement.
                        Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window
                    */
                    newWindow.GetPlacement();

            var y = 
                    /* 
                        TODO UA315_B
                        Use Microsoft.UI.Windowing.AppWindow.Move instead of RequestMoveAdjacentToWindow.
                        Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing#positioning-a-window
                    */
                    newWindow.RequestMoveAdjacentToWindow();
        }

        private 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow GetWindow()
        {
            var x = ABC.AppWindow;
            return 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                Microsoft.UI.Windowing.AppWindow.Create();
        }

        private void CreateCoreWindow()
        {
            var win = 
                    /* 
                        TODO UA315_B
                        Use Microsoft.UI.Windowing.AppWindow.Create instead of GetForCurrentThread.
                        Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                    */
                    Windows.UI.Core.CoreWindow.GetForCurrentThread();
        }

        private 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView CreateApplicationView()
        {
            return new 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView();

                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView.Hello();
        }

        private void Other()
        {
            Windows.UI.WindowManagement.AppWindowChangedEventArgs x;
            x.DidSizeChange += () => { };
        }
    }
}
