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
            var currentResourceManager = ResourceManager.Current;
            var currentResourceManager2 = Microsoft.Windows.ApplicationModel.Resources.ResourceManager.Current;
        }

        private async void CreateResourceContext()
        {
            var currentResourceManager = ResourceManager.Current;
            var resourceContext1 = ResourceContext.GetForViewIndependentUse();
            var resourceContext2 = ResourceContext.GetForCurrentView();
        }
    }
}
