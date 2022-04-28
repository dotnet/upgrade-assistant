using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Security.Credentials.UI;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.Core;

namespace TestProject.TestClasses
{
    public class InteropsCaller
    {
        private CoreDispatcher _dispatcher = Window.Current.Dispatcher;

        private async void CallDispatcher()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputVideo.SetSource(stream, _OutputFile.ContentType);
            });
        }

        private async void CallDispatcher2()
        {
            var dispatcher = Window.Current.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputVideo.SetSource(stream, _OutputFile.ContentType);
            });
        }

        private async void CallDispatcher3()
        {
            await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputVideo.SetSource(stream, _OutputFile.ContentType);
            });
        }

    }
}
