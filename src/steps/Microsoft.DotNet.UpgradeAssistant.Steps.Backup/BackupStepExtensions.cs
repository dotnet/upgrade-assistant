// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Backup;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class BackupStepExtensions
    {
        public static void AddBackupStep(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<BackupStep>();
            services.AddExtensionOption<BackupOptions>("Backup");
        }
    }
}
