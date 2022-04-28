using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Security.Credentials.UI;

namespace TestProject.TestClasses
{
    public class InteropsCaller
    {
        private async void CallUserConsentVerifier()
        {
            var x = await Windows.Security.Credentials.UI.UserConsentVerifierInterop.RequestVerificationForWindowAsync(App.WindowHandle, "Test Message");
            var y = await Windows.Security.Credentials.UI.UserConsentVerifierInterop.RequestVerificationForWindowAsync(App.WindowHandle, "Test Message");
        }
    }
}
