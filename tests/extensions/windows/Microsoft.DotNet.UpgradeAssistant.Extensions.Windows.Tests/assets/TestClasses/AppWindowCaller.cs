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
            var newWindow = await Windows.UI.WindowManagement.AppWindow.TryCreateAsync();
            await newWindow.TryShowAsync();

            var x = newWindow.GetPlacement();

            var y = newWindow.RequestMoveAdjacentToWindow();
        }

        private Windows.UI.WindowManagement.AppWindow GetWindow()
        {
            var x = ABC.AppWindow;
            return await AppWindow.TryCreateAsync();
        }

        private void CreateCoreWindow()
        {
            var win = Windows.UI.Core.CoreWindow.GetForCurrentThread();
        }

        private ApplicationView CreateApplicationView()
        {
            return new ApplicationView();
            ApplicationView.Hello();
        }

        private void Other()
        {
            Windows.UI.WindowManagement.AppWindowChangedEventArgs x;
            x.DidSizeChange += () => { };
        }
    }
}
