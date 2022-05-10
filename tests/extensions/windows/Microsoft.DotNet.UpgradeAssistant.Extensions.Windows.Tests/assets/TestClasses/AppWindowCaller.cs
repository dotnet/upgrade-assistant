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
            var newWindow = await Windows.UI.WindowManagement.AppWindow.TryCreateAsync();
            await newWindow.TryShowAsync();
        }

        private Windows.UI.WindowManagement.AppWindow GetWindow()
        {
            var x = ABC.AppWindow;
            return await AppWindow.TryCreateAsync();
        }

        private void Other()
        {
            Windows.UI.WindowManagement.AppWindowChangedEventArgs x;
            x.DidSizeChange += () => { };
        }
    }
}
