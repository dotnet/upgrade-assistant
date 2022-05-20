using System;
using System.Web;
using System.Web.Razor;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace TestProject.TestClasses
{
    public class ContentDialogCaller
    {
        private async void CallContentDialog()
        {
            var filePicker = new FileSavePicker();
            var folderPicker = new FolderPicker();
            var fileOpenPicker = this.InitializeWithWindow(new FileOpenPicker());
        }

        private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
            return obj;
        }
    }
}
