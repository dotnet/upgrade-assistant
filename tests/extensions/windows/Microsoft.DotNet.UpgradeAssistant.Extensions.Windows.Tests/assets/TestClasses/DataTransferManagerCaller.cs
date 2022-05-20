using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace TestProject.TestClasses
{
    public class DataTransferManagerCaller
    {
        private async void TransferData()
        {
            DataTransferManager.ShowShareUI();
            DataTransferManager.ShowShareUI(new ShareUIOptions());
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }
    }
}
