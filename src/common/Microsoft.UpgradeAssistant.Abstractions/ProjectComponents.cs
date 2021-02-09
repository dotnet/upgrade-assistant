using System;

namespace Microsoft.UpgradeAssistant
{
    [Flags]
    public enum ProjectComponents
    {
        None = 0,
        WindowsDesktop = 1,
        Web = 2
    }
}
