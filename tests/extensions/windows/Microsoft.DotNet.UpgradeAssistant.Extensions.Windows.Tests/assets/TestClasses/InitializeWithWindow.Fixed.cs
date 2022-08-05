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
            var filePicker = /* TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
            Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd */
        InitializeWithWindow(new FileSavePicker(), App.WindowHandle);
            var folderPicker = /* TODO You should replace 'App.WindowHandle' with the your window's handle (HWND) 
            Read more on retrieving window handle here: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd */
        InitializeWithWindow(new FolderPicker(), App.WindowHandle);
            var fileOpenPicker = this.InitializeWithWindow(new FileOpenPicker());
        }
                        private static FolderPicker InitializeWithWindow(FolderPicker obj, IntPtr windowHandle)
                        {
                            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
                            return obj;
                        }
                        private static FileSavePicker InitializeWithWindow(FileSavePicker obj, IntPtr windowHandle)
                        {
                            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
                            return obj;
                        }

        private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
            return obj;
        }
    }
}
