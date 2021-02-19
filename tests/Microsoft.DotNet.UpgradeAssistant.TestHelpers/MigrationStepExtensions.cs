// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class MigrationStepExtensions
    {
        public static void SetStatus(this MigrationStep step, MigrationStepStatus status)
        {
            var method = typeof(MigrationStep).GetProperty(nameof(MigrationStep.Status))?.GetSetMethod(nonPublic: true);

            if (method is null)
            {
                throw new InvalidOperationException("Could not find MigrationStep.Status property");
            }

            method.Invoke(step, new object[] { status });
        }
    }
}
