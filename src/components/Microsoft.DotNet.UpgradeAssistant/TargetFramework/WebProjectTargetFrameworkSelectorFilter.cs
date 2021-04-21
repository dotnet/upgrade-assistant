// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class WebProjectTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new System.ArgumentNullException(nameof(tfm));
            }

            if (tfm.Components.HasFlag(ProjectComponents.AspNet) || tfm.Components.HasFlag(ProjectComponents.AspNetCore))
            {
                tfm.TryUpdate(tfm.AppBase);
            }
        }
    }
}
