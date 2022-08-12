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
            ContentDialogResult result = /* TODO You should replace 'this' with the instance of UserControl that is ContentDialog is meant to be a part of. */SetContentDialogRoot(saveDialog, this).ShowAsync();
        }
                    private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog, UserControl control)
                    {
                        if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                        {
                            contentDialog.XamlRoot = control.Content.XamlRoot;
                        }
                        return contentDialog;
                    }
    }
}
