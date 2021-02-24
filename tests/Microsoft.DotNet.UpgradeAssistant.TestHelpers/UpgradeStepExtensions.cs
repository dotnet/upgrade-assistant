// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgradeStepExtensions
    {
        public static void SetStatus(this UpgradeStep step, UpgradeStepStatus status)
        {
            var method = typeof(UpgradeStep).GetProperty(nameof(UpgradeStep.Status))?.GetSetMethod(nonPublic: true);

            if (method is null)
            {
                throw new InvalidOperationException("Could not find UpgradeStep.Status property");
            }

            method.Invoke(step, new object[] { status });
        }
    }
}
