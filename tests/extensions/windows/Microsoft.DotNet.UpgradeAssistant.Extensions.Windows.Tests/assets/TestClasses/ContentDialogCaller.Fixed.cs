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
    public class ContentDialogCaller
    {
        private async void CallContentDialog()
        {
            ContentDialog saveDialog = new ContentDialog()
            {
                Title = "Unsaved changes",
                Content = "You have unsaved changes that will be lost if you leave this page.",
                PrimaryButtonText = "Leave this page",
                SecondaryButtonText = "Stay"
            };
            ContentDialogResult result = this.SetContentDialogRoot(saveDialog).ShowAsync();
        }
                    private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
                    {
                        if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                        {
                            contentDialog.XamlRoot = this.Content.XamlRoot;
                        }
                        return contentDialog;
                    }
    }
}
