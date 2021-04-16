// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ITargetFrameworkSelectorFilterState
    {
        IProject Project { get; }

        TargetFrameworkMoniker AppBase { get; }

        TargetFrameworkMoniker Current { get; }

        ProjectComponents Components { get; }

        bool TryUpdate(TargetFrameworkMoniker tfm);
    }
}
