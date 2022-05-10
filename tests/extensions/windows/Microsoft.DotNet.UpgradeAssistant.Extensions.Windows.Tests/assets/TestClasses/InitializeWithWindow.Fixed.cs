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
            var filePicker = this.InitializeWithWindow(new FileSavePicker());
            var folderPicker = this.InitializeWithWindow(new FolderPicker());
            var fileOpenPicker = this.InitializeWithWindow(new FileOpenPicker());
        }
                        private FolderPicker InitializeWithWindow(FolderPicker obj)
                        {
                            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
                            return obj;
                        }
                        private FileSavePicker InitializeWithWindow(FileSavePicker obj)
                        {
                            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
                            return obj;
                        }

        private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
            return obj;
        }
    }
}
