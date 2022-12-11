using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPToWinAppSDKUpgradeHelpers
{
    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [System.Runtime.InteropServices.InterfaceType(
        System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
            [System.Runtime.InteropServices.In] ref Guid riid);
        void ShowShareUIForWindow(IntPtr appWindow);
    }

    static class DataTransferManagerHelper
    {
        public static Windows.ApplicationModel.DataTransfer.DataTransferManager MarshalledDataTransferManagerFromWindow(IntPtr appWindow)
        {
            var _dtm_iid = new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);
            var result = Windows.ApplicationModel.DataTransfer.DataTransferManager.As<UWPToWinAppSDKUpgradeHelpers.IDataTransferManagerInterop>()
                .GetForWindow(appWindow, _dtm_iid);

            return WinRT.MarshalInterface<Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);
        }
    }
}
