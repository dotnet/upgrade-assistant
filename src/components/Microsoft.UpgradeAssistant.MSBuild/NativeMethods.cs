using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace AspNetMigrator.MSBuild
{
    internal static class NativeMethods
    {
        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
        public static extern int GetSetupConfiguration(
            [MarshalAs(UnmanagedType.Interface)][Out] out ISetupConfiguration configuration,
            IntPtr reserved);
    }
}
