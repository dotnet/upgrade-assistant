// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    [Flags]
    public enum ProjectComponents
    {
        None = 0,
        WindowsDesktop = 1,
        AspNet = 1 << 1,
        WinRT = 1 << 2,
        Wpf = 1 << 3,
        WinForms = 1 << 4,
        AspNetCore = 1 << 5,
        XamarinAndroid = 1 << 6,
        XamariniOS = 1 << 7,
        Maui = 1 << 8,
        MauiAndroid = 1 << 9,
        MauiiOS = 1 << 10,
        WinUI = 1 << 11,
        UWP = 1 << 12,
    }
}
