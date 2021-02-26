// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
#pragma warning disable SA1114 // Parameter list should follow declaration

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal static class NativeMethods
    {
        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
        public static extern int GetSetupConfiguration(
            [MarshalAs(UnmanagedType.Interface)][Out] out ISetupConfiguration configuration,
            IntPtr reserved);
    }
}
