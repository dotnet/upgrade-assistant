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
        Web = 1 << 1,
        WinRT = 1 << 2,
        Wpf = 1 << 3,
        WinForms = 1 << 4,
    }
}
