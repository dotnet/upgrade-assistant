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
        Other,
        None
    }
}
