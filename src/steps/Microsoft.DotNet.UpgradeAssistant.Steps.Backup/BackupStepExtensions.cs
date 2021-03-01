// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Steps.Backup;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class BackupStepExtensions
    {
        public static IServiceCollection AddBackupStep(this IServiceCollection services) =>
            services.AddUpgradeStep<BackupStep>();
    }
}
