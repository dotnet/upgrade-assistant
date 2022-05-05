using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace TestProject.TestClasses
{
    public class ContentDialogCaller
    {
        private async void CreateResourceManager()
        {
            var currentResourceManager = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            var currentResourceManager2 = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
        }

        private async void CreateResourceContext()
        {
            var currentResourceManager = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            var resourceContext1 = /*
                TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
                Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
                replace the new instance created below with correct instance.
                Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
            */new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext();
            var resourceContext2 = /*
                TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
                Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
                replace the new instance created below with correct instance.
                Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
            */new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext();
        }
    }
}
