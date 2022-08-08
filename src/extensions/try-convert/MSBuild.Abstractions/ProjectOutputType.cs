// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace MSBuild.Abstractions
{
    /// <summary>
    /// Represents the output of a project given from the OutpuType property.
    /// </summary>
    public enum ProjectOutputType
    {
        Library,
        Exe,
        WinExe,
        AppContainerExe,
        WinMdObj,
        Other,
        None
    }
}
