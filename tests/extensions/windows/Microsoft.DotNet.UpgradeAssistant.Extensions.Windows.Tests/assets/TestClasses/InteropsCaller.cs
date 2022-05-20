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

namespace TestProject.TestClasses
{
    public class InteropsCaller
    {
        private async void CallUserConsentVerifier()
        {
            var x = await UserConsentVerifier.RequestVerificationAsync("Test Message");
            var y = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync("Test Message");
        }

        private void CallDragDrop()
        {
            var a = CoreDragDropManager.GetForCurrentView();
            var b = Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.GetForCurrentView();
        }

    }
}
